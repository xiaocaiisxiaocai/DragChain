using System.Text.Json;
using DragChain.API.Sensor.Controllers;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace DragChain.API.Tests;

internal static class SensorMergeSmokeTests
{
    public static async Task RunAsync()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SensorDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var db = new SensorDbContext(options);
        await db.Database.EnsureCreatedAsync();

        db.Products.Add(new Product
        {
            Code = "sensor-smoke",
            Model = "SMOKE-1",
            Name = "感应器冒烟测试",
            Type = "photoelectric",
            Brand = "test",
            Spec = "用于验证感应器模块已迁入"
        });
        db.RbacUsers.Add(new RbacUser
        {
            EmployeeNo = "SMOKE-ADMIN",
            Name = "管理员",
            Password = RbacPasswordHasher.HashPassword("Admin-Smoke-Password-1!"),
            Role = "admin",
            Enabled = true
        });
        db.RbacUsers.Add(new RbacUser
        {
            EmployeeNo = "SMOKE-SA",
            Name = "超级管理员",
            Password = RbacPasswordHasher.HashPassword("Super-Smoke-Password-1!"),
            Role = "super_admin",
            Enabled = true
        });
        await db.SaveChangesAsync();

        var hasSmokeProduct = await db.Products.AnyAsync(product => product.Code == "sensor-smoke");
        var hasAdminUser = await db.RbacUsers.AnyAsync(user => user.EmployeeNo == "SMOKE-ADMIN");

        AssertEqual(true, hasSmokeProduct, "感应器产品表应可写入并查询");
        AssertEqual(true, hasAdminUser, "感应器用户表应可写入并查询");

        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/Trunking/calc"), "线槽计算接口必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("PUT", "/api/Trunking/saved-selection"), "线槽选型保存接口必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/Trunking/saved-selection"), "线槽选型读取接口必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/Calculation/calc"), "拖链计算接口必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/PipeLibrary"), "管线库读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/PipeModules"), "模块库读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/PipeComponents"), "元件库读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/products"), "感应器产品读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/selection-tree"), "感应器选型树读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/selection-tree/nodes/1/results"), "感应器选型结果读取必须登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/PipeLibrary"), "管线库维护不能免登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/selection-tree/nodes"), "感应器选型维护不能免登录");
        AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/rbac/users"), "权限用户接口不能免登录");
        await AssertExistingRolesReceiveNewSelectionPermissionsAsync(db);
        await AssertAdminCannotMaintainSuperAdminAsync(db);
        await AssertSelectionResultAllowsEmptyProductsAsync(db);

        Console.WriteLine("PASS Sensor module DbContext smoke test");
    }

    private static async Task AssertSelectionResultAllowsEmptyProductsAsync(SensorDbContext db)
    {
        var controller = new SelectionTreeController(db, new TestWebHostEnvironment());

        var nodeResult = await controller.CreateNode(new SaveSelectionNodeDto
        {
            Name = "无产品选型分类"
        });
        var nodeJson = JsonSerializer.Serialize(((OkObjectResult)nodeResult).Value);
        using var nodeDocument = JsonDocument.Parse(nodeJson);
        var nodeId = nodeDocument.RootElement.GetProperty("Id").GetInt32();

        var createResult = await controller.CreateNodeResult(nodeId, new SaveSelectionResultDto
        {
            Note = "仅维护作用说明，暂不绑定产品",
            Products = []
        });

        var ok = createResult as OkObjectResult;
        AssertEqual(true, ok != null, "添加选型结果时产品不是必选项");
        var resultJson = JsonSerializer.Serialize(ok!.Value);
        using var resultDocument = JsonDocument.Parse(resultJson);
        AssertEqual(0, resultDocument.RootElement.GetProperty("Products").GetArrayLength(), "空产品选型结果必须保存为空产品列表");
    }

    private static async Task AssertAdminCannotMaintainSuperAdminAsync(SensorDbContext db)
    {
        var adminToken = await LoginAsync(db, "SMOKE-ADMIN", "Admin-Smoke-Password-1!");
        var adminController = BuildAuthController(db, adminToken);

        var createDenied = await adminController.CreateUser(new SaveUserDto("S0002", "新超级管理员", "Denied-Admin-Password-1!", "super_admin", true));
        AssertEqual(403, ((ObjectResult)createDenied).StatusCode ?? 0, "管理员不能创建超级管理员");

        var superAdmin = await db.RbacUsers.SingleAsync(user => user.EmployeeNo == "SMOKE-SA");
        var updateSuperDenied = await adminController.UpdateUser(
            superAdmin.Id,
            new SaveUserDto(superAdmin.EmployeeNo, "被管理员修改", "Denied-Update-Password-1!", "super_admin", false));
        AssertEqual(403, ((ObjectResult)updateSuperDenied).StatusCode ?? 0, "管理员不能修改超级管理员");

        var normalUser = new RbacUser
        {
            EmployeeNo = "user1",
            Name = "普通用户",
            Password = RbacPasswordHasher.HashPassword("Normal-User-Password-1!"),
            Role = "user",
            Enabled = true
        };
        db.RbacUsers.Add(normalUser);
        await db.SaveChangesAsync();

        var promoteDenied = await adminController.UpdateUser(
            normalUser.Id,
            new SaveUserDto(normalUser.EmployeeNo, normalUser.Name, null, "super_admin", true));
        AssertEqual(403, ((ObjectResult)promoteDenied).StatusCode ?? 0, "管理员不能把普通用户提升为超级管理员");

        var superToken = await LoginAsync(db, "SMOKE-SA", "Super-Smoke-Password-1!");
        var superController = BuildAuthController(db, superToken);
        var updateAllowed = await superController.UpdateUser(
            superAdmin.Id,
            new SaveUserDto(superAdmin.EmployeeNo, "超级管理员", null, "super_admin", true));
        AssertEqual(typeof(NoContentResult), updateAllowed.GetType(), "超级管理员可以维护超级管理员账号");
    }

    private static async Task AssertExistingRolesReceiveNewSelectionPermissionsAsync(SensorDbContext db)
    {
        db.RbacRolePermissions.RemoveRange(db.RbacRolePermissions);
        db.RbacRolePermissions.Add(new RbacRolePermission
        {
            Role = "user",
            PermissionCode = "api:selector:read"
        });
        db.RbacRolePermissions.Add(new RbacRolePermission
        {
            Role = "admin",
            PermissionCode = "api:selector:read"
        });
        await db.SaveChangesAsync();

        await RbacMigrator.MigrateAsync(db);

        var userPermissions = await db.RbacRolePermissions
            .Where(item => item.Role == "user")
            .Select(item => item.PermissionCode)
            .ToListAsync();
        var adminPermissions = await db.RbacRolePermissions
            .Where(item => item.Role == "admin")
            .Select(item => item.PermissionCode)
            .ToListAsync();

        AssertEqual(true, userPermissions.Contains("api:trunking:read"), "旧数据库普通用户角色必须补上线槽选型使用权限");
        AssertEqual(true, userPermissions.Contains("api:chain:read"), "旧数据库普通用户角色必须补上拖链选型使用权限");
        AssertEqual(true, userPermissions.Contains("api:pipe:read"), "旧数据库普通用户角色必须补上管线库读取权限");
        AssertEqual(true, adminPermissions.Contains("api:trunking:read"), "旧数据库管理员角色必须补上线槽选型使用权限");

        var removedPermission = await db.RbacRolePermissions.SingleAsync(item =>
            item.Role == "user" && item.PermissionCode == "api:trunking:read");
        db.RbacRolePermissions.Remove(removedPermission);
        await db.SaveChangesAsync();
        await RbacMigrator.MigrateAsync(db);
        AssertEqual(false, await db.RbacRolePermissions.AnyAsync(item =>
            item.Role == "user" && item.PermissionCode == "api:trunking:read"), "一次性迁移完成后不能补回管理员显式删除的权限");
    }

    private static async Task<string> LoginAsync(SensorDbContext db, string employeeNo, string password)
    {
        var result = await BuildAuthController(db).Login(new LoginDto(employeeNo, null, password));
        var json = JsonSerializer.Serialize(((OkObjectResult)result).Value);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("登录结果缺少 accessToken");
    }

    private static AuthController BuildAuthController(SensorDbContext db, string? accessToken = null)
    {
        var controller = new AuthController(db)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        if (!string.IsNullOrWhiteSpace(accessToken))
            controller.Request.Headers.Authorization = $"Bearer {accessToken}";
        return controller;
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}：期望 {expected}，实际 {actual}");
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "DragChain.API.Tests";
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
