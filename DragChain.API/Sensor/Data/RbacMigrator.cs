using System.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Sensor.Data;

public static class RbacMigrator
{
    private const string SecureCredentialMigrationKey = "2026-08-rbac-secure-credentials-v1";
    private const string RbacModuleMigrationKey = "2026-06-rbac-selection-permissions";
    private static readonly HashSet<string> LegacySeededEmployeeNos = new(StringComparer.OrdinalIgnoreCase)
    {
        "S0001",
        "admin",
        "U0001"
    };

    public static async Task MigrateAsync(SensorDbContext db)
    {
        await EnsureBaseSchemaAsync(db);

        var connection = db.Database.GetDbConnection() as SqliteConnection
            ?? throw new InvalidOperationException("RBAC 迁移仅支持 SQLite 数据库");
        var closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone)
            await connection.OpenAsync();

        await using var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        var enlisted = false;
        try
        {
            await db.Database.UseTransactionAsync(transaction);
            enlisted = true;

            await EnsureSecurityStampColumnAsync(db);
            await ApplySecureCredentialMigrationAsync(db);
            await EnsurePasswordsAndSecurityStampsAsync(db);
            await EnsureRolePermissionsAsync(db, "super_admin");
            await EnsureRolePermissionsAsync(db, "admin");
            await EnsureRolePermissionsAsync(db, "editor");
            await EnsureRolePermissionsAsync(db, "user");
            await MigrateRbacModulePermissionsAsync(db);

            await transaction.CommitAsync();
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync();
            }
            catch
            {
                // 保留原始迁移异常；未提交事务在释放时仍会回滚。
            }
            throw;
        }
        finally
        {
            if (enlisted)
                await db.Database.UseTransactionAsync(null);
            if (closeWhenDone)
                await connection.CloseAsync();
        }
    }

    private static async Task EnsureBaseSchemaAsync(SensorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS RbacUsers (
                Id INTEGER NOT NULL CONSTRAINT PK_RbacUsers PRIMARY KEY AUTOINCREMENT,
                EmployeeNo TEXT NOT NULL,
                Name TEXT NOT NULL,
                Password TEXT NOT NULL,
                SecurityStamp TEXT NOT NULL DEFAULT '',
                Role TEXT NOT NULL DEFAULT 'user',
                Enabled INTEGER NOT NULL DEFAULT 1
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_RbacUsers_EmployeeNo
            ON RbacUsers (EmployeeNo)
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS RbacRolePermissions (
                Id INTEGER NOT NULL CONSTRAINT PK_RbacRolePermissions PRIMARY KEY AUTOINCREMENT,
                Role TEXT NOT NULL,
                PermissionCode TEXT NOT NULL
            )
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS IX_RbacRolePermissions_Role_PermissionCode
            ON RbacRolePermissions (Role, PermissionCode)
            """);

        await EnsureMigrationHistoryTableAsync(db);
    }

    private static async Task EnsureSecurityStampColumnAsync(SensorDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var closeWhenDone = connection.State != ConnectionState.Open;
        if (closeWhenDone)
            await connection.OpenAsync();

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('RbacUsers') WHERE name = 'SecurityStamp'";
            var exists = Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
            if (!exists)
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE RbacUsers ADD COLUMN SecurityStamp TEXT NOT NULL DEFAULT ''");
            }
        }
        finally
        {
            if (closeWhenDone)
                await connection.CloseAsync();
        }
    }

    private static async Task EnsureMigrationHistoryTableAsync(SensorDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS RbacMigrationHistory (
                Key TEXT NOT NULL CONSTRAINT PK_RbacMigrationHistory PRIMARY KEY,
                AppliedAt TEXT NOT NULL
            )
            """);
    }

    private static async Task ApplySecureCredentialMigrationAsync(SensorDbContext db)
    {
        if (!await TryClaimMigrationAsync(db, SecureCredentialMigrationKey)) return;

        var users = await db.RbacUsers.ToListAsync();

        if (users.Count == 0)
        {
            var password = RequireBootstrapPassword();
            var configuredEmployeeNo = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO");
            var employeeNo = string.IsNullOrWhiteSpace(configuredEmployeeNo) ? "admin" : configuredEmployeeNo.Trim();
            var configuredName = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME");

            users.Add(new RbacUser
            {
                EmployeeNo = employeeNo,
                Name = string.IsNullOrWhiteSpace(configuredName) ? employeeNo : configuredName.Trim(),
                Password = RbacPasswordHasher.HashPassword(password),
                SecurityStamp = RbacPasswordHasher.CreateSecurityStamp(),
                Role = "super_admin",
                Enabled = true
            });
            db.RbacUsers.Add(users[^1]);
        }
        else
        {
            RemediateLegacySeededAccounts(db, users);
        }

        HardenStoredCredentials(users);
        await db.SaveChangesAsync();
    }

    private static void RemediateLegacySeededAccounts(
        SensorDbContext db,
        List<RbacUser> users)
    {
        var legacyUsers = users
            .Where(user => LegacySeededEmployeeNos.Contains(user.EmployeeNo.Trim()))
            .ToList();
        if (legacyUsers.Count == 0) return;

        var hasEnabledNonLegacySuperAdmin = users.Any(user =>
            user.Enabled
            && user.Role == "super_admin"
            && !LegacySeededEmployeeNos.Contains(user.EmployeeNo.Trim()));

        if (hasEnabledNonLegacySuperAdmin)
        {
            DisableLegacyUsers(legacyUsers, except: null);
            return;
        }

        var password = RequireBootstrapPassword();
        var target = ResolveBootstrapRotationTarget(db, users, legacyUsers);
        var configuredName = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME");
        if (!string.IsNullOrWhiteSpace(configuredName))
            target.Name = configuredName.Trim();

        target.Password = RbacPasswordHasher.HashPassword(password);
        target.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
        target.Role = "super_admin";
        target.Enabled = true;
        DisableLegacyUsers(legacyUsers, target);
    }

    private static RbacUser ResolveBootstrapRotationTarget(
        SensorDbContext db,
        List<RbacUser> users,
        List<RbacUser> legacyUsers)
    {
        var configuredEmployeeNo = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO");
        if (!string.IsNullOrWhiteSpace(configuredEmployeeNo))
        {
            var employeeNo = configuredEmployeeNo.Trim();
            var existing = users.FirstOrDefault(user =>
                string.Equals(user.EmployeeNo.Trim(), employeeNo, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return existing;

            var configuredName = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_NAME");
            var created = new RbacUser
            {
                EmployeeNo = employeeNo,
                Name = string.IsNullOrWhiteSpace(configuredName) ? employeeNo : configuredName.Trim()
            };
            users.Add(created);
            db.RbacUsers.Add(created);
            return created;
        }

        return legacyUsers.FirstOrDefault(user => user.Role == "super_admin")
            ?? throw new InvalidOperationException(
                "旧 RBAC 用户库不存在可轮换的超级管理员，必须显式配置 DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO。");
    }

    private static void DisableLegacyUsers(IEnumerable<RbacUser> legacyUsers, RbacUser? except)
    {
        foreach (var legacyUser in legacyUsers)
        {
            if (ReferenceEquals(legacyUser, except)) continue;
            legacyUser.Enabled = false;
            legacyUser.Password = RbacPasswordHasher.HashPassword(
                RbacPasswordHasher.CreateSecurityStamp());
            legacyUser.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
        }
    }

    private static string RequireBootstrapPassword()
    {
        var password = Environment.GetEnvironmentVariable("DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD");
        var validationError = RbacPasswordHasher.ValidateStrongPassword(password, required: true);
        if (validationError != null)
        {
            throw new InvalidOperationException(
                $"RBAC 安全初始化需要有效的 DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD：{validationError}");
        }

        return password!;
    }

    private static async Task EnsurePasswordsAndSecurityStampsAsync(SensorDbContext db)
    {
        var users = await db.RbacUsers.ToListAsync();
        if (!HardenStoredCredentials(users)) return;
        await db.SaveChangesAsync();
    }

    private static bool HardenStoredCredentials(IEnumerable<RbacUser> users)
    {
        var changed = false;
        foreach (var user in users)
        {
            if (!RbacPasswordHasher.IsHashedPassword(user.Password))
            {
                user.Password = RbacPasswordHasher.HashPassword(user.Password);
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(user.SecurityStamp))
            {
                user.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
                changed = true;
            }
        }

        return changed;
    }

    private static async Task<bool> TryClaimMigrationAsync(SensorDbContext db, string migrationKey)
    {
        var inserted = await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO RbacMigrationHistory (Key, AppliedAt)
            VALUES ({migrationKey}, CURRENT_TIMESTAMP)
            ON CONFLICT(Key) DO NOTHING
            """);
        return inserted == 1;
    }

    private static async Task EnsureRolePermissionsAsync(SensorDbContext db, string role)
    {
        var hasAny = await db.RbacRolePermissions.AnyAsync(item => item.Role == role);
        if (hasAny) return;

        foreach (var code in RbacPermissionCatalog.DefaultPermissions(role))
        {
            db.RbacRolePermissions.Add(new RbacRolePermission
            {
                Role = role,
                PermissionCode = code
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task MigrateRbacModulePermissionsAsync(SensorDbContext db)
    {
        if (!await TryClaimMigrationAsync(db, RbacModuleMigrationKey)) return;

        var legacyCodes = new[] { "menu:sensor:users", "page:sensor:users" };
        var legacyRows = await db.RbacRolePermissions
            .Where(item => legacyCodes.Contains(item.PermissionCode))
            .ToListAsync();

        await CopyPermissionAsync(db, "menu:sensor:users", "menu:rbac");
        await CopyPermissionAsync(db, "page:sensor:users", "page:rbac:users");
        await CopyPermissionAsync(db, "page:sensor:users", "page:rbac:roles");

        await EnsurePermissionAsync(db, "super_admin", "menu:rbac");
        await EnsurePermissionAsync(db, "super_admin", "page:rbac:users");
        await EnsurePermissionAsync(db, "super_admin", "page:rbac:roles");
        await EnsurePermissionAsync(db, "admin", "menu:rbac");
        await EnsurePermissionAsync(db, "admin", "page:rbac:users");
        await EnsurePermissionAsync(db, "admin", "page:rbac:roles");
        await EnsureSelectionUsagePermissionsAsync(db, "admin");
        await EnsureSelectionUsagePermissionsAsync(db, "user");

        db.RbacRolePermissions.RemoveRange(legacyRows);
        await db.SaveChangesAsync();
    }

    private static async Task EnsureSelectionUsagePermissionsAsync(SensorDbContext db, string role)
    {
        foreach (var code in new[]
        {
            "api:selector:read",
            "api:products:read",
            "api:taxonomy:read",
            "api:trunking:read",
            "api:chain:read",
            "api:pipe:read"
        })
        {
            await EnsurePermissionAsync(db, role, code);
        }
    }

    private static async Task CopyPermissionAsync(SensorDbContext db, string oldCode, string newCode)
    {
        var roles = await db.RbacRolePermissions
            .Where(item => item.PermissionCode == oldCode)
            .Select(item => item.Role)
            .Distinct()
            .ToListAsync();

        foreach (var role in roles)
        {
            await EnsurePermissionAsync(db, role, newCode);
        }
    }

    private static async Task EnsurePermissionAsync(SensorDbContext db, string role, string code)
    {
        var explicitlyEmpty = db.RbacRolePermissions.Local.Any(item =>
                item.Role == role && item.PermissionCode == RbacPermissionCatalog.EmptyPermissionMarker)
            || await db.RbacRolePermissions.AnyAsync(item =>
                item.Role == role && item.PermissionCode == RbacPermissionCatalog.EmptyPermissionMarker);
        if (explicitlyEmpty) return;

        var exists = db.RbacRolePermissions.Local.Any(item => item.Role == role && item.PermissionCode == code)
            || await db.RbacRolePermissions.AnyAsync(item => item.Role == role && item.PermissionCode == code);
        if (exists) return;

        db.RbacRolePermissions.Add(new RbacRolePermission
        {
            Role = role,
            PermissionCode = code
        });
    }
}
