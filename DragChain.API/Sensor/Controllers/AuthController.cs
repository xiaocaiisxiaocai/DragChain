using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Security;
using DragChain.API.Sensor.Services;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    public const string SessionItemKey = "__RbacSession";
    private const string AccessTokenKind = "access";
    private const string RefreshTokenKind = "refresh";
    private static readonly JsonSerializerOptions TokenJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConcurrentDictionary<string, RbacSession> Sessions = new();
    private static readonly ConcurrentDictionary<string, RbacSession> RefreshSessions = new();
    private static readonly ConcurrentDictionary<string, LoginFailureState> LoginFailures = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object SigningKeyLock = new();
    private static byte[]? _signingKey;
    private const int MaxFailedLoginAttempts = 5;
    private static readonly TimeSpan LoginFailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LoginLockoutDuration = TimeSpan.FromMinutes(15);
    private readonly SensorDbContext _context;

    public AuthController(SensorDbContext context) => _context = context;

    public static void ConfigureSigningKey(byte[] signingKey)
    {
        ArgumentNullException.ThrowIfNull(signingKey);
        if (signingKey.Length < 32)
            throw new ArgumentException("签名密钥至少需要 32 字节。", nameof(signingKey));
        lock (SigningKeyLock)
        {
            _signingKey = signingKey.ToArray();
        }
    }

    public static bool TryGetSession(string accessToken, out RbacSession session)
    {
        if (Sessions.TryGetValue(accessToken, out session!))
        {
            if (session.Expires > DateTime.UtcNow)
                return true;
            Sessions.TryRemove(accessToken, out _);
        }

        session = null!;
        return false;
    }

    public static string[] GetSessionPermissions(RbacSession session) => session.Permissions;

    public static async Task<string[]> GetRolePermissionsAsync(SensorDbContext db, string role)
    {
        if (!RbacPermissionCatalog.IsKnownRole(role)) return [];
        if (role == "super_admin") return RbacPermissionCatalog.DefaultPermissions("super_admin");

        var storedPermissions = await db.RbacRolePermissions
            .Where(item => item.Role == role)
            .Select(item => item.PermissionCode)
            .ToArrayAsync();

        if (storedPermissions.Length == 0)
            return RbacPermissionCatalog.DefaultPermissions(role);

        return storedPermissions
            .Where(code => code != RbacPermissionCatalog.EmptyPermissionMarker)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    [HttpPost("/login")]
    [HttpPost("/api/auth/login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {
        CleanupExpiredSessions();
        CleanupExpiredLoginFailures();
        var employeeNo = (dto.EmployeeNo ?? dto.Username ?? string.Empty).Trim();
        var loginKey = BuildLoginFailureKey(employeeNo);
        if (IsLoginBlocked(loginKey))
            return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "登录尝试过多，请稍后重试" });

        var user = await _context.RbacUsers.FirstOrDefaultAsync(item => item.EmployeeNo == employeeNo);
        if (user == null || !user.Enabled ||
            !RbacPasswordHasher.VerifyPassword(user.Password, dto.Password ?? string.Empty, out var needsRehash))
        {
            RecordLoginFailure(loginKey);
            return Unauthorized(new { success = false, message = "工号或密码错误" });
        }

        ClearLoginFailures(loginKey);
        if (needsRehash)
        {
            user.Password = RbacPasswordHasher.HashPassword(dto.Password!);
            user.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
            await _context.SaveChangesAsync();
        }

        var expires = DateTime.UtcNow.AddHours(12);
        var permissions = await GetRolePermissionsAsync(user.Role);
        var session = new RbacSession(user.Id, user.EmployeeNo, user.Name, user.Role, permissions, user.SecurityStamp, expires);
        var accessToken = CreateSignedToken(session, AccessTokenKind);
        var refreshToken = CreateSignedToken(session, RefreshTokenKind);
        Sessions[accessToken] = session;
        RefreshSessions[refreshToken] = session;

        return Ok(new
        {
            success = true,
            data = BuildUserResult(user, accessToken, refreshToken, permissions, expires)
        });
    }

    [HttpPost("/refresh-token")]
    [HttpPost("/api/auth/refresh-token")]
    public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        CleanupExpiredSessions();
        if (string.IsNullOrWhiteSpace(dto.RefreshToken) ||
            !TryConsumeRefreshSession(dto.RefreshToken, out var oldSession))
        {
            return Unauthorized(new { success = false, message = "登录态已失效" });
        }

        var expires = DateTime.UtcNow.AddHours(12);
        var user = await _context.RbacUsers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == oldSession.UserId);
        if (user == null || !user.Enabled ||
            !string.Equals(user.SecurityStamp, oldSession.SecurityStamp, StringComparison.Ordinal))
        {
            RevokeSessionsForUser(oldSession.UserId);
            return Unauthorized(new { success = false, message = "登录态已失效" });
        }

        var permissions = await GetRolePermissionsAsync(user.Role);
        var session = new RbacSession(user.Id, user.EmployeeNo, user.Name, user.Role, permissions, user.SecurityStamp, expires);

        var accessToken = CreateSignedToken(session, AccessTokenKind);
        var refreshToken = CreateSignedToken(session, RefreshTokenKind);

        Sessions[accessToken] = session;
        RefreshSessions[refreshToken] = session;

        return Ok(new
        {
            success = true,
            data = BuildUserResult(user, accessToken, refreshToken, permissions, expires)
        });
    }

    [HttpGet("/api/rbac/users")]
    public async Task<ActionResult> GetUsers()
    {
        var users = await _context.RbacUsers
            .OrderBy(user => user.Id)
            .Select(user => new
            {
                user.Id,
                user.EmployeeNo,
                user.Name,
                user.Role,
                user.Enabled
            })
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("/api/rbac/users/export")]
    public async Task<IActionResult> ExportUsers()
    {
        var users = await _context.RbacUsers.OrderBy(user => user.Id).ToListAsync();
        var bytes = RbacUserWorkbookService.CreateWorkbook(users);
        var fileName = $"rbac-users-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, RbacUserWorkbookService.ExcelContentType, fileName);
    }

    [HttpPost("/api/rbac/users/import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> ImportUsers([FromForm] IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "请选择要导入的 xlsx 文件" });

        await using var stream = file.OpenReadStream();
        var parseResult = RbacUserWorkbookService.ParseWorkbook(stream);
        if (parseResult.Errors.Count > 0)
            return BadRequest(new { message = "导入文件校验失败", errors = parseResult.Errors });

        var users = await _context.RbacUsers.ToListAsync();
        var byEmployeeNo = users
            .GroupBy(user => user.EmployeeNo.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();
        var seenEmployeeNos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var affectedUsers = new List<(RbacUser User, bool RevokeSessions)>();
        var created = 0;
        var updated = 0;

        foreach (var row in parseResult.Rows)
        {
            if (!seenEmployeeNos.Add(row.EmployeeNo))
                errors.Add($"第 {row.RowNumber} 行工号「{row.EmployeeNo}」在文件中重复");

            byEmployeeNo.TryGetValue(row.EmployeeNo, out var user);
            if ((user?.Role == "super_admin" || row.Role == "super_admin") && !CurrentUserIsSuperAdmin())
                errors.Add($"第 {row.RowNumber} 行只有超级管理员可以维护超级管理员账号");

            if (!TryNormalizeRole(row.Role, out _))
                errors.Add($"第 {row.RowNumber} 行角色无效");

            var passwordError = RbacPasswordHasher.ValidateStrongPassword(row.Password, required: user == null);
            if (passwordError != null)
                errors.Add($"第 {row.RowNumber} 行{passwordError}");
        }

        if (errors.Count > 0)
            return errors.Any(error => error.Contains("只有超级管理员", StringComparison.Ordinal))
                ? SuperAdminOnly()
                : BadRequest(new { message = "导入文件校验失败", errors });

        foreach (var row in parseResult.Rows)
        {
            byEmployeeNo.TryGetValue(row.EmployeeNo, out var user);
            if (user == null)
            {
                user = new RbacUser
                {
                    EmployeeNo = row.EmployeeNo,
                    Password = RbacPasswordHasher.HashPassword(row.Password),
                    SecurityStamp = RbacPasswordHasher.CreateSecurityStamp()
                };
                _context.RbacUsers.Add(user);
                byEmployeeNo[row.EmployeeNo] = user;
                created++;
            }
            else
            {
                updated++;
            }

            user.EmployeeNo = row.EmployeeNo;
            user.Name = row.Name;
            TryNormalizeRole(row.Role, out var normalizedRole);
            user.Role = normalizedRole;
            var enabledChanged = user.Enabled != row.Enabled;
            user.Enabled = row.Enabled;
            var passwordChanged = !string.IsNullOrWhiteSpace(row.Password);
            if (!string.IsNullOrWhiteSpace(row.Password))
                user.Password = RbacPasswordHasher.HashPassword(row.Password);
            if (passwordChanged || enabledChanged)
                user.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
            affectedUsers.Add((user, passwordChanged || enabledChanged || !user.Enabled));
        }

        await _context.SaveChangesAsync();

        foreach (var (user, revokeSessions) in affectedUsers)
        {
            if (revokeSessions)
                RevokeSessionsForUser(user.Id);
            else
                await RefreshSessionsForUserAsync(user);
        }

        return Ok(new { created, updated, total = parseResult.Rows.Count });
    }

    [HttpGet("/api/rbac/permissions")]
    public ActionResult GetPermissions()
    {
        return Ok(RbacPermissionCatalog.Items);
    }

    [HttpGet("/api/rbac/roles")]
    public async Task<ActionResult> GetRolePermissions()
    {
        var rows = await _context.RbacRolePermissions
            .OrderBy(item => item.Role)
            .ThenBy(item => item.PermissionCode)
            .ToListAsync();

        return Ok(rows
            .GroupBy(item => item.Role)
            .Select(group => new
            {
                Role = group.Key,
                Permissions = group
                    .Select(item => item.PermissionCode)
                    .Where(code => code != RbacPermissionCatalog.EmptyPermissionMarker)
                    .ToArray()
            }));
    }

    [HttpPut("/api/rbac/roles/{role}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(string role, [FromBody] SaveRolePermissionsDto dto)
    {
        if (!CurrentUserIsSuperAdmin())
            return SuperAdminOnly();

        if (!TryNormalizeRole(role, out role))
            return BadRequest(new { message = "角色无效" });
        if (role == "super_admin") return BadRequest(new { message = "超级管理员固定拥有全部权限" });

        var allowedCodes = RbacPermissionCatalog.Items.Select(item => item.Code).ToHashSet();
        var permissions = (dto.Permissions ?? []).Where(allowedCodes.Contains).Distinct().ToArray();

        var oldRows = await _context.RbacRolePermissions.Where(item => item.Role == role).ToListAsync();
        _context.RbacRolePermissions.RemoveRange(oldRows);
        var permissionsToStore = permissions.Length == 0
            ? new[] { RbacPermissionCatalog.EmptyPermissionMarker }
            : permissions;
        foreach (var code in permissionsToStore)
        {
            _context.RbacRolePermissions.Add(new RbacRolePermission
            {
                Role = role,
                PermissionCode = code
            });
        }
        await _context.SaveChangesAsync();
        RefreshSessionsForRole(role, permissions);
        return NoContent();
    }

    [HttpPost("/api/rbac/users")]
    public async Task<ActionResult> CreateUser([FromBody] SaveUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EmployeeNo) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "工号和姓名不能为空" });

        var employeeNo = dto.EmployeeNo.Trim();
        var name = dto.Name.Trim();
        if (!TryNormalizeRole(dto.Role, out var requestedRole))
            return BadRequest(new { message = "角色无效" });
        if (requestedRole == "super_admin" && !CurrentUserIsSuperAdmin())
            return SuperAdminOnly();
        var passwordError = RbacPasswordHasher.ValidateStrongPassword(dto.Password, required: true);
        if (passwordError != null)
            return BadRequest(new { message = passwordError });

        var normalizedEmployeeNo = employeeNo.ToLower();
        var exists = await _context.RbacUsers.AnyAsync(user => user.EmployeeNo.ToLower() == normalizedEmployeeNo);
        if (exists) return Conflict(new { message = "工号已存在" });

        var user = new RbacUser
        {
            EmployeeNo = employeeNo,
            Name = name,
            Password = RbacPasswordHasher.HashPassword(dto.Password!),
            Role = requestedRole,
            Enabled = dto.Enabled,
            SecurityStamp = RbacPasswordHasher.CreateSecurityStamp()
        };
        _context.RbacUsers.Add(user);
        await _context.SaveChangesAsync();
        return Ok(ToUserDto(user));
    }

    [HttpPut("/api/rbac/users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] SaveUserDto dto)
    {
        var user = await _context.RbacUsers.FindAsync(id);
        if (user == null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.EmployeeNo) || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "工号和姓名不能为空" });
        if (!TryNormalizeRole(dto.Role, out var requestedRole))
            return BadRequest(new { message = "角色无效" });
        if ((user.Role == "super_admin" || requestedRole == "super_admin") && !CurrentUserIsSuperAdmin())
            return SuperAdminOnly();
        var passwordError = RbacPasswordHasher.ValidateStrongPassword(dto.Password, required: false);
        if (passwordError != null)
            return BadRequest(new { message = passwordError });

        var employeeNo = dto.EmployeeNo.Trim();
        var normalizedEmployeeNo = employeeNo.ToLower();
        var duplicate = await _context.RbacUsers.AnyAsync(item =>
            item.Id != id && item.EmployeeNo.ToLower() == normalizedEmployeeNo);
        if (duplicate) return Conflict(new { message = "工号已存在" });

        var passwordChanged = !string.IsNullOrWhiteSpace(dto.Password);
        var enabledChanged = user.Enabled != dto.Enabled;
        user.EmployeeNo = employeeNo;
        user.Name = dto.Name.Trim();
        if (passwordChanged) user.Password = RbacPasswordHasher.HashPassword(dto.Password!);
        user.Role = requestedRole;
        user.Enabled = dto.Enabled;
        if (passwordChanged || enabledChanged)
            user.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
        await _context.SaveChangesAsync();
        if (!user.Enabled || passwordChanged || enabledChanged)
            RevokeSessionsForUser(user.Id);
        else
            await RefreshSessionsForUserAsync(user);
        return NoContent();
    }

    private async Task<string[]> GetRolePermissionsAsync(string role)
    {
        return await GetRolePermissionsAsync(_context, role);
    }

    private static object BuildUserResult(
        RbacUser user,
        string accessToken,
        string refreshToken,
        string[] permissions,
        DateTime expires)
    {
        return new
        {
            avatar = "",
            username = user.EmployeeNo,
            nickname = user.Name,
            roles = new[] { user.Role },
            permissions,
            accessToken,
            refreshToken,
            expires
        };
    }

    private static object ToUserDto(RbacUser user) => new
    {
        user.Id,
        user.EmployeeNo,
        user.Name,
        user.Role,
        user.Enabled
    };

    private static void RefreshSessionsForRole(string role, string[] permissions)
    {
        foreach (var token in Sessions.Keys.ToList())
        {
            var session = Sessions[token];
            if (session.Role == role)
            {
                Sessions[token] = session with { Permissions = permissions };
            }
        }
        foreach (var token in RefreshSessions.Keys.ToList())
        {
            var session = RefreshSessions[token];
            if (session.Role == role)
            {
                RefreshSessions[token] = session with { Permissions = permissions };
            }
        }
    }

    private async Task RefreshSessionsForUserAsync(RbacUser user)
    {
        var permissions = await GetRolePermissionsAsync(user.Role);
        foreach (var token in Sessions.Keys.ToList())
        {
            if (!Sessions.TryGetValue(token, out var session) || session.UserId != user.Id) continue;
            Sessions[token] = session with
            {
                EmployeeNo = user.EmployeeNo,
                Name = user.Name,
                Role = user.Role,
                Permissions = permissions,
                SecurityStamp = user.SecurityStamp
            };
        }
        foreach (var token in RefreshSessions.Keys.ToList())
        {
            if (!RefreshSessions.TryGetValue(token, out var session) || session.UserId != user.Id) continue;
            RefreshSessions[token] = session with
            {
                EmployeeNo = user.EmployeeNo,
                Name = user.Name,
                Role = user.Role,
                Permissions = permissions,
                SecurityStamp = user.SecurityStamp
            };
        }
    }

    private static void RevokeSessionsForUser(int userId)
    {
        foreach (var token in Sessions.Keys.ToList())
        {
            if (Sessions.TryGetValue(token, out var session) && session.UserId == userId)
                Sessions.TryRemove(token, out _);
        }
        foreach (var token in RefreshSessions.Keys.ToList())
        {
            if (RefreshSessions.TryGetValue(token, out var session) && session.UserId == userId)
                RefreshSessions.TryRemove(token, out _);
        }
    }

    private static void CleanupExpiredSessions()
    {
        var now = DateTime.UtcNow;
        foreach (var (token, session) in Sessions)
        {
            if (session.Expires <= now)
                Sessions.TryRemove(token, out _);
        }
        foreach (var (token, session) in RefreshSessions)
        {
            if (session.Expires <= now)
                RefreshSessions.TryRemove(token, out _);
        }
    }

    private static bool TryNormalizeRole(string? role, out string normalizedRole)
    {
        normalizedRole = role?.Trim().ToLowerInvariant() switch
        {
            "super_admin" => "super_admin",
            "admin" => "admin",
            "editor" => "editor",
            "user" => "user",
            _ => string.Empty
        };
        return normalizedRole.Length > 0;
    }

    private bool CurrentUserIsSuperAdmin() => ResolveCurrentSession()?.Role == "super_admin";

    private RbacSession? ResolveCurrentSession()
    {
        if (HttpContext.Items.TryGetValue(SessionItemKey, out var value) && value is RbacSession requestSession)
            return requestSession;

        var auth = Request.Headers.Authorization.ToString();
        const string bearer = "Bearer ";
        if (!auth.StartsWith(bearer, StringComparison.OrdinalIgnoreCase)) return null;

        var token = auth[bearer.Length..].Trim();
        return TryGetSession(token, out var session) ? session : null;
    }

    private static bool TryConsumeRefreshSession(string refreshToken, out RbacSession session)
    {
        if (RefreshSessions.TryRemove(refreshToken, out session!) && session.Expires > DateTime.UtcNow)
            return true;

        session = null!;
        return false;
    }

    private static string CreateSignedToken(RbacSession session, string kind)
    {
        var payload = new SignedTokenPayload(
            kind,
            Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            session.UserId,
            session.EmployeeNo,
            session.Name,
            session.Role,
            session.Permissions,
            session.SecurityStamp,
            new DateTimeOffset(session.Expires).ToUnixTimeMilliseconds());
        var payloadText = JsonSerializer.Serialize(payload, TokenJsonOptions);
        var payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadText));
        var signaturePart = Base64UrlEncode(Sign(payloadPart));
        return $"{payloadPart}.{signaturePart}";
    }

    private static byte[] Sign(string payloadPart)
    {
        using var hmac = new HMACSHA256(GetConfiguredSigningKey());
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
    }

    private static byte[] GetConfiguredSigningKey()
    {
        if (_signingKey != null) return _signingKey;
        lock (SigningKeyLock)
        {
            _signingKey ??= GetSigningKey();
            return _signingKey;
        }
    }

    private static byte[] GetSigningKey()
    {
        var key = Environment.GetEnvironmentVariable("DRAGCHAIN_AUTH_SIGNING_KEY");
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        return AuthSigningKeyProvider.Resolve(key, environmentName);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private string BuildLoginFailureKey(string employeeNo)
    {
        var remoteAddress = ControllerContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "local";
        return $"{remoteAddress}|{employeeNo}";
    }

    private static bool IsLoginBlocked(string key)
    {
        if (!LoginFailures.TryGetValue(key, out var state)) return false;
        if (state.LockedUntil is { } lockedUntil && lockedUntil > DateTime.UtcNow) return true;
        if (DateTime.UtcNow - state.WindowStarted <= LoginFailureWindow) return false;
        LoginFailures.TryRemove(key, out _);
        return false;
    }

    private static void RecordLoginFailure(string key)
    {
        var now = DateTime.UtcNow;
        LoginFailures.AddOrUpdate(
            key,
            _ => new LoginFailureState(1, now, null),
            (_, state) =>
            {
                if (now - state.WindowStarted > LoginFailureWindow)
                    return new LoginFailureState(1, now, null);

                var count = state.Count + 1;
                var lockedUntil = count >= MaxFailedLoginAttempts
                    ? now.Add(LoginLockoutDuration)
                    : state.LockedUntil;
                return new LoginFailureState(count, state.WindowStarted, lockedUntil);
            });
    }

    private static void ClearLoginFailures(string key) => LoginFailures.TryRemove(key, out _);

    private static void CleanupExpiredLoginFailures()
    {
        var now = DateTime.UtcNow;
        foreach (var (key, state) in LoginFailures)
        {
            var lockExpired = state.LockedUntil is { } lockedUntil && lockedUntil <= now;
            var windowExpired = state.LockedUntil == null && now - state.WindowStarted > LoginFailureWindow;
            if (lockExpired || windowExpired)
                LoginFailures.TryRemove(key, out _);
        }
    }

    private ObjectResult SuperAdminOnly() =>
        StatusCode(StatusCodes.Status403Forbidden, new { message = "只有超级管理员可以维护超级管理员账号" });
}

public record LoginDto(string? EmployeeNo, string? Username, string Password);
public record RefreshTokenDto(string? RefreshToken);
public record SaveUserDto(string EmployeeNo, string Name, string? Password, string Role, bool Enabled);
public record SaveRolePermissionsDto(string[] Permissions);
public record RbacSession(
    int UserId,
    string EmployeeNo,
    string Name,
    string Role,
    string[] Permissions,
    string SecurityStamp,
    DateTime Expires);
public record SignedTokenPayload(
    string Kind,
    string TokenId,
    int UserId,
    string EmployeeNo,
    string Name,
    string Role,
    string[] Permissions,
    string SecurityStamp,
    long ExpiresUnixMs);
public record LoginFailureState(int Count, DateTime WindowStarted, DateTime? LockedUntil);
