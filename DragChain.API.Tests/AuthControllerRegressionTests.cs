using DragChain.API.Sensor.Controllers;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragChain.API.Tests;

internal static class AuthControllerRegressionTests
{
    public static async Task RunAsync()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"dragchain-auth-controller-{Guid.NewGuid():N}.db");
        try
        {
            await using var db = CreateContext(dbPath);
            await db.Database.EnsureCreatedAsync();

            var superAdmin = new RbacUser
            {
                EmployeeNo = "AUTH-SA",
                Name = "安全测试超管",
                Password = RbacPasswordHasher.HashPassword("Super-Admin-Password-1!"),
                Role = "super_admin",
                Enabled = true,
                SecurityStamp = RbacPasswordHasher.CreateSecurityStamp()
            };
            var editor = new RbacUser
            {
                EmployeeNo = "AUTH-EDITOR",
                Name = "安全测试编辑",
                Password = RbacPasswordHasher.HashPassword("Editor-Password-1!"),
                Role = "editor",
                Enabled = true,
                SecurityStamp = RbacPasswordHasher.CreateSecurityStamp()
            };
            db.RbacUsers.AddRange(superAdmin, editor);
            db.RbacRolePermissions.Add(new RbacRolePermission
            {
                Role = "editor",
                PermissionCode = "api:products:read"
            });
            await db.SaveChangesAsync();

            await AssertMiddlewarePipelineAsync(db, editor);
            await AssertUserAndPermissionMutationsAsync(db, superAdmin, editor);
            Console.WriteLine("PASS auth controller and middleware regression tests");
        }
        finally
        {
            TryDelete(dbPath);
        }
    }

    private static async Task AssertMiddlewarePipelineAsync(SensorDbContext db, RbacUser editor)
    {
        var nextCalled = false;
        var middleware = new RbacMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var anonymousContext = NewHttpContext("GET", "/api/products");
        await middleware.InvokeAsync(anonymousContext, db);
        AssertEqual(StatusCodes.Status401Unauthorized, anonymousContext.Response.StatusCode, "未认证 API 请求必须返回 401");
        AssertEqual(false, nextCalled, "未认证请求不能进入后续管道");

        nextCalled = false;
        var publicContext = NewHttpContext("POST", "/api/auth/login");
        await middleware.InvokeAsync(publicContext, db);
        AssertEqual(true, nextCalled, "登录兼容端点必须公开放行");

        var editorToken = (await LoginWithTokensAsync(db, editor.EmployeeNo, "Editor-Password-1!")).AccessToken;
        nextCalled = false;
        var allowedContext = NewHttpContext("GET", "/api/products", editorToken);
        await middleware.InvokeAsync(allowedContext, db);
        AssertEqual(true, nextCalled, "具备权限的请求必须进入后续管道");

        nextCalled = false;
        var forbiddenContext = NewHttpContext("POST", "/api/products", editorToken);
        await middleware.InvokeAsync(forbiddenContext, db);
        AssertEqual(StatusCodes.Status403Forbidden, forbiddenContext.Response.StatusCode, "缺少权限的请求必须返回 403");
        AssertEqual(false, nextCalled, "权限不足请求不能进入后续管道");

        nextCalled = false;
        var tamperedContext = NewHttpContext("GET", "/api/products", editorToken + "tampered");
        await middleware.InvokeAsync(tamperedContext, db);
        AssertEqual(StatusCodes.Status401Unauthorized, tamperedContext.Response.StatusCode, "篡改 token 必须返回 401");
    }

    private static async Task AssertUserAndPermissionMutationsAsync(
        SensorDbContext db,
        RbacUser superAdmin,
        RbacUser editor)
    {
        var superToken = (await LoginWithTokensAsync(db, superAdmin.EmployeeNo, "Super-Admin-Password-1!")).AccessToken;
        var editorTokens = await LoginWithTokensAsync(db, editor.EmployeeNo, "Editor-Password-1!");
        var controller = BuildController(db, superToken);

        var invalidRole = await controller.CreateUser(
            new SaveUserDto("AUTH-BAD-ROLE", "非法角色", "Valid-Password-1!", "owner", true));
        AssertEqual(nameof(BadRequestObjectResult), invalidRole.GetType().Name, "非法角色必须返回 400，不能静默降级");

        var missingPassword = await controller.CreateUser(
            new SaveUserDto("AUTH-NO-PASSWORD", "缺密码", null, "user", true));
        AssertEqual(nameof(BadRequestObjectResult), missingPassword.GetType().Name, "新增用户必须显式设置密码");

        var createResult = await controller.CreateUser(
            new SaveUserDto("AUTH-NEW-EDITOR", "新编辑", "New-Editor-Password-1!", "editor", true));
        var createOk = createResult as OkObjectResult;
        AssertEqual(true, createOk != null, "超级管理员必须能创建 editor 用户");
        AssertEqual(null, createOk!.Value!.GetType().GetProperty("Password"), "创建接口不能返回密码字段");
        var created = await db.RbacUsers.SingleAsync(item => item.EmployeeNo == "AUTH-NEW-EDITOR");
        AssertEqual(true, RbacPasswordHasher.VerifyPassword(created.Password, "New-Editor-Password-1!", out _), "新增密码必须哈希保存");

        var emptyPermissionsResult = await controller.UpdateRolePermissions(
            "editor",
            new SaveRolePermissionsDto([]));
        AssertEqual(nameof(NoContentResult), emptyPermissionsResult.GetType().Name, "超级管理员必须能显式清空 editor 权限");
        AssertEqual(0, (await AuthController.GetRolePermissionsAsync(db, "editor")).Length, "显式空权限不能回退默认权限");
        AssertEqual(true, await db.RbacRolePermissions.AnyAsync(item =>
            item.Role == "editor" && item.PermissionCode == RbacPermissionCatalog.EmptyPermissionMarker), "空权限必须持久化哨兵");

        var invalidRoleUpdate = await controller.UpdateRolePermissions(
            "owner",
            new SaveRolePermissionsDto([]));
        AssertEqual(nameof(BadRequestObjectResult), invalidRoleUpdate.GetType().Name, "非法角色权限更新必须返回 400");

        var updateResult = await controller.UpdateUser(
            editor.Id,
            new SaveUserDto(editor.EmployeeNo, editor.Name, "Rotated-Editor-Password-1!", "editor", true));
        AssertEqual(nameof(NoContentResult), updateResult.GetType().Name, "密码更新必须成功");
        AssertEqual(false, AuthController.TryGetSession(editorTokens.AccessToken, out _), "密码更新后旧 accessToken 必须被吊销");
        var revokedRefresh = await BuildController(db).RefreshToken(new RefreshTokenDto(editorTokens.RefreshToken));
        AssertEqual(nameof(UnauthorizedObjectResult), revokedRefresh.GetType().Name, "密码更新后旧 refreshToken 必须被吊销");

        var postRotationTokens = await LoginWithTokensAsync(db, editor.EmployeeNo, "Rotated-Editor-Password-1!");
        editor.SecurityStamp = RbacPasswordHasher.CreateSecurityStamp();
        await db.SaveChangesAsync();
        AssertEqual(true, AuthController.TryGetSession(postRotationTokens.AccessToken, out _), "竞态测试必须保留字典中的旧会话");

        var stampMiddleware = new RbacMiddleware(_ => Task.CompletedTask);
        var staleStampContext = NewHttpContext("GET", "/api/products", postRotationTokens.AccessToken);
        await stampMiddleware.InvokeAsync(staleStampContext, db);
        AssertEqual(StatusCodes.Status401Unauthorized, staleStampContext.Response.StatusCode, "安全戳变化后即使漏掉字典吊销也必须返回 401");
        var staleStampRefresh = await BuildController(db).RefreshToken(new RefreshTokenDto(postRotationTokens.RefreshToken));
        AssertEqual(nameof(UnauthorizedObjectResult), staleStampRefresh.GetType().Name, "安全戳变化后漏网 refreshToken 也必须失效");

        var corruptRoleUser = new RbacUser
        {
            EmployeeNo = "AUTH-CORRUPT-ROLE",
            Name = "损坏角色用户",
            Password = RbacPasswordHasher.HashPassword("Corrupt-Role-Password-1!"),
            Role = "owner",
            Enabled = true,
            SecurityStamp = RbacPasswordHasher.CreateSecurityStamp()
        };
        db.RbacUsers.Add(corruptRoleUser);
        db.RbacRolePermissions.Add(new RbacRolePermission
        {
            Role = corruptRoleUser.Role,
            PermissionCode = "api:products:read"
        });
        await db.SaveChangesAsync();
        AssertEqual(0, (await AuthController.GetRolePermissionsAsync(db, corruptRoleUser.Role)).Length, "未知持久化角色即使残留权限记录也必须失败关闭为零权限");
        var corruptRoleToken = (await LoginWithTokensAsync(db, corruptRoleUser.EmployeeNo, "Corrupt-Role-Password-1!")).AccessToken;
        var corruptRoleContext = NewHttpContext("GET", "/api/products", corruptRoleToken);
        await stampMiddleware.InvokeAsync(corruptRoleContext, db);
        AssertEqual(StatusCodes.Status403Forbidden, corruptRoleContext.Response.StatusCode, "未知持久化角色不能继承普通用户读取权限");
    }

    private static async Task<LoginTokens> LoginWithTokensAsync(SensorDbContext db, string employeeNo, string password)
    {
        var result = await BuildController(db).Login(new LoginDto(employeeNo, null, password));
        var ok = result as OkObjectResult ?? throw new InvalidOperationException($"测试账号 {employeeNo} 登录失败");
        var data = ok.Value?.GetType().GetProperty("data")?.GetValue(ok.Value)
            ?? throw new InvalidOperationException("登录结果缺少 data");
        var accessToken = data.GetType().GetProperty("accessToken")?.GetValue(data)?.ToString()
            ?? throw new InvalidOperationException("登录结果缺少 accessToken");
        var refreshToken = data.GetType().GetProperty("refreshToken")?.GetValue(data)?.ToString()
            ?? throw new InvalidOperationException("登录结果缺少 refreshToken");
        return new LoginTokens(accessToken, refreshToken);
    }

    private static AuthController BuildController(SensorDbContext db, string? accessToken = null)
    {
        var controller = new AuthController(db)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        if (!string.IsNullOrWhiteSpace(accessToken))
            controller.Request.Headers.Authorization = $"Bearer {accessToken}";
        return controller;
    }

    private static DefaultHttpContext NewHttpContext(string method, string path, string? accessToken = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        if (!string.IsNullOrWhiteSpace(accessToken))
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        return context;
    }

    private static SensorDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<SensorDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
        return new SensorDbContext(options);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // 连接释放延迟时交由系统清理临时数据库。
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message}：期望 {expected}，实际 {actual}");
    }

    private sealed record LoginTokens(string AccessToken, string RefreshToken);
}
