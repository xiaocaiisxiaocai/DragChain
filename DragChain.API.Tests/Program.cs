using DragChain.API.Data;
using DragChain.API.Controllers;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;
using DragChain.API.Tests;
using DragChain.API.Sensor.Controllers;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Security;
using DragChain.API.Sensor.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security;
using System.Text;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
Environment.SetEnvironmentVariable("DRAGCHAIN_AUTH_SIGNING_KEY", "Regression-Signing-Key-32-Bytes-Minimum!");

await SecurityRegressionTests.RunAsync();
await AuthControllerRegressionTests.RunAsync();

AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/trunking/calc"), "线槽计算接口必须登录后才能访问");
AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("POST", "/api/calculation/calc"), "拖链计算接口必须登录后才能访问");
AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/products"), "感应器产品接口必须登录后才能访问");
AssertEqual(false, RbacPermissionCatalog.IsPublicRequest("GET", "/api/trunking/saved-selections"), "保存选型列表必须登录后才能访问");
AssertEqual(true, RbacPermissionCatalog.IsPublicRequest("GET", "/api/health"), "健康检查保留公开访问");

var defaultUserPermissions = RbacPermissionCatalog.DefaultPermissions("user");
AssertEqual(true, defaultUserPermissions.Contains("api:trunking:read"), "普通用户默认必须能使用线槽选型读取和计算接口");
AssertEqual(true, defaultUserPermissions.Contains("api:chain:read"), "普通用户默认必须能使用拖链选型读取和计算接口");
AssertEqual(true, defaultUserPermissions.Contains("api:pipe:read"), "普通用户默认必须能读取管线、模块和元件库用于选型");
AssertEqual("api:trunking:read", MatchApiPermission("GET", "/api/trunking"), "线槽型录读取必须绑定读取权限");
AssertEqual("api:trunking:read", MatchApiPermission("POST", "/api/trunking/calc"), "线槽计算必须绑定读取权限");
AssertEqual("api:trunking:read", MatchApiPermission("PUT", "/api/trunking/saved-selection"), "保存线槽选型必须绑定线槽使用权限");
AssertEqual("api:trunking:write", MatchApiPermission("POST", "/api/trunking"), "线槽型录新增必须绑定维护权限");
AssertEqual("api:trunking:write", MatchApiPermission("PUT", "/api/trunking/settings"), "线槽设置修改必须绑定维护权限");
AssertEqual("api:chain:read", MatchApiPermission("POST", "/api/calculation/calc"), "拖链计算必须绑定读取权限");

var tempDir = Path.Combine(Path.GetTempPath(), $"dragchain-trunking-test-{Guid.NewGuid():N}");
Directory.CreateDirectory(tempDir);
var dbPath = Path.Combine(tempDir, "test.db");

await using (var context = CreateContext(dbPath))
{
    await context.Database.EnsureCreatedAsync();
    Seed(context);

    var service = new TrunkingCalculationService(context);
    var pipes = new List<PipeItemDto>
    {
        new() { PipeTypeId = 1, Qty = 1 },
        new() { PipeTypeId = 2, Qty = 1 },
        new() { PipeTypeId = 3, Qty = 1 },
        new() { PipeTypeId = 4, Qty = 1 },
        new() { PipeTypeId = 5, Qty = 1 }
    };

    var relaxed = await service.CalculateAsync(new TrunkingCalcRequest { FillRatio = 0.5m, Pipes = pipes });
    var strict = await service.CalculateAsync(new TrunkingCalcRequest { FillRatio = 0.2m, Pipes = pipes });
    var defaulted = await service.CalculateAsync(new TrunkingCalcRequest { FillRatio = 0m, Pipes = pipes });

    AssertEqual(0.48m, Math.Round(relaxed.ActualFillRatio, 4), "请求上限不再覆盖型号上限时实际填充率");
    AssertEqual(0.48m, Math.Round(strict.ActualFillRatio, 4), "请求上限变化不能改变推荐线槽的实际填充率");
    AssertEqual("TK-25×25", strict.WeakSide?.SelectedTrunking?.Model, "请求上限变化不能覆盖线槽型号自己的上限");
    AssertEqual(0.6m, new TrunkingCalcRequest().FillRatio, "线槽请求默认有效利用率上限");
    AssertEqual(0.6m, defaulted.FillRatio, "未传有效上限时服务必须回退到 60%");

    context.TrunkingCatalog.Single(t => t.Id == 1).FillRatioLimit = 0.10m;
    context.TrunkingCatalog.Single(t => t.Id == 2).FillRatioLimit = 0.20m;
    context.TrunkingCatalog.Single(t => t.Id == 3).FillRatioLimit = 0.60m;
    await context.SaveChangesAsync();

    var catalogLimited = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.9m,
        Pipes =
        [
            new() { PipeTypeId = 1, Qty = 1 },
            new() { PipeTypeId = 2, Qty = 1 }
        ]
    });

    AssertEqual("TK-40×25", catalogLimited.WeakSide?.SelectedTrunking?.Model, "推荐线槽必须使用各型号自己的有效利用率上限");
    AssertEqual(0.2m, catalogLimited.WeakSide?.FillRatio, "分区结果必须返回推荐型号自己的有效利用率上限");
    AssertEqual(0.2m, catalogLimited.WeakSide?.SelectedTrunking?.FillRatioLimit, "线槽 DTO 必须包含型号有效利用率上限");
    AssertEqual(false, catalogLimited.WeakSide?.MatchResults.Single(t => t.Id == 1).OkFill, "型号自己的上限不足时必须不可用");
    AssertEqual(true, catalogLimited.WeakSide?.MatchResults.Single(t => t.Id == 2).OkFill, "型号自己的上限足够时必须可用");

    foreach (var trunking in context.TrunkingCatalog)
    {
        trunking.FillRatioLimit = 0.60m;
    }
    await context.SaveChangesAsync();

    var overLimit = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.1m,
        Pipes =
        [
            new() { PipeTypeId = 1, Qty = 1 },
            new() { PipeTypeId = 2, Qty = 1 }
        ]
    });

    AssertEqual(0.1152m, Math.Round(overLimit.ActualFillRatio, 4), "实际填充率应按当前基准线槽显示为 11.5%");
    AssertEqual("ok", overLimit.ResultStatus, "请求上限不再覆盖型号上限");
    AssertEqual("ok", overLimit.WeakSide?.ResultStatus, "弱电侧必须按线槽型号上限判定");

    var autoMatchedSlot = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.1m,
        Slots =
        [
            new()
            {
                Id = "slot-auto-match",
                Name = "未选线槽自动匹配",
                Layout = "leftRight",
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1, Layer = "top" },
                    new() { PipeTypeId = 2, Qty = 1, Layer = "top" }
                ]
            }
        ]
    });

    AssertEqual("ok", autoMatchedSlot.ResultStatus, "槽位未选择线槽时应自动匹配可用线槽");
    AssertEqual(2, autoMatchedSlot.Slots.Count, "一个槽位必须生成上下两条线槽段");
    AssertEqual("TK-25×25", autoMatchedSlot.Slots[0].Sections[0].SelectedTrunking?.Model, "槽位未选择线槽时应返回满足型号上限的最小线槽");
    AssertEqual(0.1152m, Math.Round(autoMatchedSlot.Slots[0].Sections[0].ActualFillRatio, 4), "自动匹配后实际填充率必须按匹配线槽计算");

    AssertEqual("topToBottom", new TrunkingCalcRequest().SlotOrder, "旧请求缺少槽位顺序字段时必须保持原始顺序");

    var legacyOrderSlots = await service.CalculateAsync(new TrunkingCalcRequest
    {
        Slots = CreateSlotOrderTestSlots(useSections: false)
    });
    var explicitTopToBottomSlots = await service.CalculateAsync(new TrunkingCalcRequest
    {
        SlotOrder = "topToBottom",
        Slots = CreateSlotOrderTestSlots(useSections: false)
    });
    AssertEqual("槽位1上", legacyOrderSlots.Slots[0].Name, "旧请求必须以传入的第一个槽位作为最上方槽位");
    AssertEqual("槽位1下 + 槽位2上", legacyOrderSlots.Slots[1].Name, "旧请求的中间段必须保持传入顺序");
    AssertEqual("槽位2下", legacyOrderSlots.Slots[2].Name, "旧请求必须以传入的最后一个槽位作为最下方槽位");
    AssertSlotCalculationsEquivalent(legacyOrderSlots, explicitTopToBottomSlots, "显式 topToBottom 必须与旧请求缺省行为一致");

    var topDownSlots = await service.CalculateAsync(new TrunkingCalcRequest
    {
        SlotOrder = "bottomToTop",
        Slots = CreateSlotOrderTestSlots(useSections: false)
    });
    var sectionEncodedTopDownSlots = await service.CalculateAsync(new TrunkingCalcRequest
    {
        SlotOrder = "bottomToTop",
        Slots = CreateSlotOrderTestSlots(useSections: true)
    });

    AssertEqual(3, topDownSlots.Slots.Count, "两个槽位按编号向上叠加时必须生成三条线槽段");
    AssertEqual(1, topDownSlots.SideSlots.Count, "多个槽位必须合并为一组贯通左右竖向线槽结果");
    AssertEqual("槽位2上", topDownSlots.Slots[0].Name, "最上方线槽段必须是最高编号槽位的上层");
    AssertEqual("槽位2下 + 槽位1上", topDownSlots.Slots[1].Name, "中间线槽段必须合并上方槽位下层和下方槽位上层");
    AssertEqual("槽位1下", topDownSlots.Slots[2].Name, "最下方线槽段必须是槽位1下层");
    AssertEqual(64m, topDownSlots.Slots[0].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位2上左侧必须包含弱电");
    AssertEqual(0m, topDownSlots.Slots[0].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位2上右侧无强电时必须为 0");
    AssertEqual(36m, topDownSlots.Slots[1].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "中间段左侧必须包含槽位2下加槽位1上的弱电");
    AssertEqual(128m, topDownSlots.Slots[1].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "中间段右侧必须包含槽位2下加槽位1上的强电");
    AssertEqual(36m, topDownSlots.Slots[2].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位1下左侧必须包含弱电");
    AssertEqual(0m, topDownSlots.Slots[2].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位1下右侧无强电时必须为 0");
    AssertEqual("左右竖向线槽", topDownSlots.SideSlots[0].Name, "侧边结果必须对应贯通左右竖向线槽");
    AssertEqual(136m, topDownSlots.SideSlots[0].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "左侧竖向线槽必须汇总所有槽位上下层弱电");
    AssertEqual(128m, topDownSlots.SideSlots[0].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "右侧竖向线槽必须汇总所有槽位上下层强电");
    AssertSlotCalculationsEquivalent(topDownSlots, sectionEncodedTopDownSlots, "Pipes 与 Sections 两种真实请求路径必须得到相同槽位结果");

    var controller = new TrunkingController(context, service);
    context.AppSettings.Add(new AppSetting
    {
        Key = "TrunkingSavedSelection",
        Value = """
            {"name":"旧版保存数据","savedAt":"2026-01-01T00:00:00Z","request":{"fillRatio":0.6,"pipes":[],"slots":[{"id":"legacy-slot","name":"旧槽位","pipes":[{"pipeTypeId":1,"qty":1,"layer":"top"}],"sections":[]}]}}
            """
    });
    await context.SaveChangesAsync();
    var legacySaved = await controller.GetSavedSelection();
    AssertEqual("topToBottom", legacySaved.Value?.Request.SlotOrder, "旧保存数据缺少 slotOrder 字段时必须按 topToBottom 兼容加载");
    var legacySavedSetting = await context.AppSettings.FindAsync("TrunkingSavedSelection");
    context.AppSettings.Remove(legacySavedSetting!);
    await context.SaveChangesAsync();

    var saved = await controller.SaveSelection(new TrunkingSavedSelectionDto
    {
        Name = "测试线槽选型",
        Request = new TrunkingCalcRequest
        {
            Slots =
            [
                new()
                {
                    Id = "slot-1",
                    Name = "槽位1",
                    Sections =
                    [
                        new()
                        {
                            Key = "top",
                            Label = "上层",
                            Pipes = [new() { PipeTypeId = 1, Qty = 1, Layer = "top" }]
                        }
                    ]
                }
            ]
        },
        Result = topDownSlots
    });
    AssertEqual("测试线槽选型", saved.Value?.Name, "保存线槽选型必须返回保存名称");
    AssertEqual(true, await context.AppSettings.AnyAsync(setting => setting.Key == "TrunkingSavedSelection"), "线槽选型必须保存到数据库 AppSettings");

    var loaded = await controller.GetSavedSelection();
    AssertEqual("测试线槽选型", loaded.Value?.Name, "保存后的线槽选型必须能从数据库读取");

    var savedList = await controller.GetSavedSelections();
    var savedId = savedList.Value?.Single().Id;
    AssertEqual(false, string.IsNullOrWhiteSpace(savedId), "保存列表必须给旧数据补稳定 ID");

    var deleteResult = await controller.DeleteSavedSelection(savedId!);
    AssertEqual("NoContentResult", deleteResult.GetType().Name, "旧单条兼容数据必须能从保存列表删除");

    var afterDelete = await controller.GetSavedSelections();
    AssertEqual(0, afterDelete.Value?.Count, "删除后保存列表不能再从旧单条 Key 恢复");
}

var sensorDbPath = Path.Combine(tempDir, "sensor-rbac.db");
await using (var sensorContext = CreateSensorContext(sensorDbPath))
{
    await sensorContext.Database.EnsureCreatedAsync();
    sensorContext.RbacUsers.AddRange(
        new RbacUser { EmployeeNo = "SA001", Name = "超管", Password = "Legacy-Root-Password-1!", Role = "super_admin", Enabled = true },
        new RbacUser { EmployeeNo = "U001", Name = "旧用户", Password = "Legacy-User-Password-1!", Role = "user", Enabled = true }
    );
    await sensorContext.SaveChangesAsync();
    await RbacMigrator.MigrateAsync(sensorContext);

    var exportBytes = RbacUserWorkbookService.CreateWorkbook(await sensorContext.RbacUsers.OrderBy(user => user.Id).ToListAsync());
    var exported = RbacUserWorkbookService.ParseWorkbook(new MemoryStream(exportBytes));
    AssertEqual(0, exported.Errors.Count, "用户导出的 xlsx 必须能再次解析");
    AssertEqual(2, exported.Rows.Count, "用户导出必须包含当前用户");
    AssertEqual("", exported.Rows.Single(row => row.EmployeeNo == "U001").Password, "用户导出不能带出明文密码");

    var importBytes = SetWorkbookPassword(RbacUserWorkbookService.CreateWorkbook(
    [
        new RbacUser { EmployeeNo = "U001", Name = "改名用户", Password = "", Role = "admin", Enabled = false },
        new RbacUser { EmployeeNo = "U002", Name = "新用户", Password = "New-User-Password-1!", Role = "editor", Enabled = true }
    ]), 3, "New-User-Password-1!");

    var controller = new AuthController(sensorContext)
    {
        ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        }
    };
    var loginResult = await controller.Login(new LoginDto("SA001", null, "Legacy-Root-Password-1!"));
    var loginOk = loginResult as Microsoft.AspNetCore.Mvc.OkObjectResult;
    var loginData = loginOk?.Value?.GetType().GetProperty("data")?.GetValue(loginOk.Value);
    var accessToken = loginData?.GetType().GetProperty("accessToken")?.GetValue(loginData)?.ToString();
    var refreshToken = loginData?.GetType().GetProperty("refreshToken")?.GetValue(loginData)?.ToString();
    AssertEqual(true, AuthController.TryGetSession(accessToken!, out var activeSession), "登录后的 accessToken 必须能识别");
    AssertEqual("SA001", activeSession.EmployeeNo, "登录态必须保留当前用户工号");

    var currentSuperAdmin = await sensorContext.RbacUsers.SingleAsync(user => user.EmployeeNo == "SA001");
    currentSuperAdmin.Name = "刷新后的超管名称";
    await sensorContext.SaveChangesAsync();
    var refreshResult = await new AuthController(sensorContext).RefreshToken(new RefreshTokenDto(refreshToken));
    var refreshOk = refreshResult as Microsoft.AspNetCore.Mvc.OkObjectResult;
    var refreshData = refreshOk?.Value?.GetType().GetProperty("data")?.GetValue(refreshOk.Value);
    var rotatedAccessToken = refreshData?.GetType().GetProperty("accessToken")?.GetValue(refreshData)?.ToString();
    var rotatedRefreshToken = refreshData?.GetType().GetProperty("refreshToken")?.GetValue(refreshData)?.ToString();
    var refreshedRoles = refreshData?.GetType().GetProperty("roles")?.GetValue(refreshData) as string[];
    var refreshedNickname = refreshData?.GetType().GetProperty("nickname")?.GetValue(refreshData)?.ToString();
    AssertEqual("OkObjectResult", refreshResult.GetType().Name, "有效 refreshToken 必须能轮换登录态");
    AssertEqual(false, refreshToken == rotatedRefreshToken, "轮换后的 refreshToken 必须包含随机标识并与旧 token 不同");
    AssertEqual("刷新后的超管名称", refreshedNickname, "刷新响应必须从数据库返回最新用户元数据");
    AssertEqual(true, refreshedRoles?.Contains("super_admin") == true, "刷新响应必须返回服务端当前完整角色");

    var replayResult = await new AuthController(sensorContext).RefreshToken(new RefreshTokenDto(refreshToken));
    AssertEqual("UnauthorizedObjectResult", replayResult.GetType().Name, "旧 refreshToken 轮换后不能重放");

    ClearInMemoryAuthSessions();
    AssertEqual(false, AuthController.TryGetSession(rotatedAccessToken!, out _), "进程内会话清空后 accessToken 必须失效");
    var restartRefreshResult = await new AuthController(sensorContext).RefreshToken(new RefreshTokenDto(rotatedRefreshToken));
    AssertEqual("UnauthorizedObjectResult", restartRefreshResult.GetType().Name, "进程内会话清空后 refreshToken 必须失效");

    var reloginResult = await controller.Login(new LoginDto("SA001", null, "Legacy-Root-Password-1!"));
    var reloginOk = reloginResult as Microsoft.AspNetCore.Mvc.OkObjectResult;
    var reloginData = reloginOk?.Value?.GetType().GetProperty("data")?.GetValue(reloginOk.Value);
    accessToken = reloginData?.GetType().GetProperty("accessToken")?.GetValue(reloginData)?.ToString();
    controller.ControllerContext.HttpContext.Request.Headers.Authorization = $"Bearer {accessToken}";

    var result = await controller.ImportUsers(new FormFile(new MemoryStream(importBytes), 0, importBytes.Length, "file", "users.xlsx"));
    AssertEqual("OkObjectResult", result.GetType().Name, "超级管理员可通过接口导入用户");

    var updated = await sensorContext.RbacUsers.SingleAsync(user => user.EmployeeNo == "U001");
    var created = await sensorContext.RbacUsers.SingleAsync(user => user.EmployeeNo == "U002");
    AssertEqual("改名用户", updated.Name, "导入时必须按工号更新已有用户");
    AssertEqual(true, RbacPasswordHasher.VerifyPassword(updated.Password, "Legacy-User-Password-1!", out _), "导入更新用户时密码为空不能覆盖原密码哈希");
    AssertEqual("admin", updated.Role, "导入时必须更新用户角色");
    AssertEqual(false, updated.Enabled, "导入时必须更新启用状态");
    AssertEqual("editor", created.Role, "导入必须支持 editor 角色");
    AssertEqual(true, RbacPasswordHasher.VerifyPassword(created.Password, "New-User-Password-1!", out _), "导入新增用户密码必须哈希保存");

    var missingPasswordBytes = RbacUserWorkbookService.CreateWorkbook(
    [
        new RbacUser { EmployeeNo = "U003", Name = "缺密码用户", Password = "", Role = "user", Enabled = true }
    ]);
    var missingPasswordResult = await controller.ImportUsers(new FormFile(new MemoryStream(missingPasswordBytes), 0, missingPasswordBytes.Length, "file", "users.xlsx"));
    AssertEqual("BadRequestObjectResult", missingPasswordResult.GetType().Name, "导入新增用户不得使用固定默认密码");

    var nonSuperImportBytes = RbacUserWorkbookService.CreateWorkbook(
    [
        new RbacUser { EmployeeNo = "SA002", Name = "新超管", Password = "Another-Admin-Password-1!", Role = "super_admin", Enabled = true }
    ]);
    controller.ControllerContext.HttpContext.Request.Headers.Authorization = "";
    var forbidden = await controller.ImportUsers(new FormFile(new MemoryStream(nonSuperImportBytes), 0, nonSuperImportBytes.Length, "file", "users.xlsx"));
    AssertEqual("ObjectResult", forbidden.GetType().Name, "非超级管理员不能通过导入维护超级管理员账号");
}

await using (var context = CreateContext(dbPath))
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    SeedWithoutLargestTrunking(context);

    await CatalogSeeder.SeedAsync(context);

    var trunkingModels = await context.TrunkingCatalog
        .OrderBy(t => t.Id)
        .Select(t => t.Model)
        .ToListAsync();

    AssertEqual(true, trunkingModels.Contains("TK-100×100"), "已有旧线槽型录时启动种子必须补齐新增默认线槽");
    AssertEqual(9, trunkingModels.Count, "补齐默认线槽不能重复插入已有型号");
}

await using (var context = CreateContext(dbPath))
{
    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();
    Seed(context);

    context.PipeModules.Add(new PipeModule
    {
        Id = 1,
        Name = "上层模块",
        Description = "",
        Items =
        [
            new() { PipeTypeId = 1, Qty = 1, Layer = "top" },
            new() { PipeTypeId = 1, Qty = 2, Layer = "bottom" },
            new() { PipeTypeId = 2, Qty = 1 }
        ]
    });
    context.PipeComponents.Add(new PipeComponent
    {
        Id = 1,
        Name = "默认上层元件",
        Description = "",
        Items =
        [
            new() { PipeTypeId = 1, Qty = 1, Layer = "bottom" },
            new() { PipeTypeId = 2, Qty = 1 }
        ]
    });
    await context.SaveChangesAsync();
}

await using (var context = CreateContext(dbPath))
{
    var moduleLayers = await context.PipeModuleItems
        .Where(item => item.PipeModuleId == 1)
        .OrderBy(item => item.Id)
        .Select(item => item.Layer)
        .ToListAsync();
    var componentLayers = await context.PipeComponentItems
        .Where(item => item.PipeComponentId == 1)
        .OrderBy(item => item.Id)
        .Select(item => item.Layer)
        .ToListAsync();

    AssertEqual(3, moduleLayers.Count, "同一管线选择不同上下标识时不能被合并掉");
    AssertEqual("top", moduleLayers[0], "模块内管线必须保存上标识");
    AssertEqual("bottom", moduleLayers[1], "模块内同一管线也必须能单独保存下标识");
    AssertEqual("top", moduleLayers[2], "模块内管线默认必须是上");
    AssertEqual("bottom", componentLayers[0], "元件内管线必须保存下标识");
    AssertEqual("top", componentLayers[1], "元件内管线默认必须是上");
}

try
{
    Directory.Delete(tempDir, recursive: true);
}
catch
{
    // SQLite 连接释放有时滞后，临时目录留给系统清理即可。
}

Console.WriteLine("PASS TrunkingCalculationService 实际填充率不随填充率上限变化");

await SensorMergeSmokeTests.RunAsync();

static List<TrunkingSlotRequestDto> CreateSlotOrderTestSlots(bool useSections)
{
    if (!useSections)
    {
        return
        [
            new()
            {
                Id = "slot-1",
                Name = "槽位1",
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1, Layer = "top" },
                    new() { PipeTypeId = 7, Qty = 1, Layer = "top" },
                    new() { PipeTypeId = 2, Qty = 1, Layer = "bottom" }
                ]
            },
            new()
            {
                Id = "slot-2",
                Name = "槽位2",
                Pipes =
                [
                    new() { PipeTypeId = 3, Qty = 1, Layer = "top" },
                    new() { PipeTypeId = 7, Qty = 1, Layer = "bottom" }
                ]
            }
        ];
    }

    return
    [
        new()
        {
            Id = "slot-1",
            Name = "槽位1",
            Sections =
            [
                new()
                {
                    Key = "top",
                    Label = "上层",
                    Pipes =
                    [
                        new() { PipeTypeId = 1, Qty = 1 },
                        new() { PipeTypeId = 7, Qty = 1 }
                    ]
                },
                new()
                {
                    Key = "bottom",
                    Label = "下层",
                    Pipes = [new() { PipeTypeId = 2, Qty = 1 }]
                }
            ]
        },
        new()
        {
            Id = "slot-2",
            Name = "槽位2",
            Sections =
            [
                new()
                {
                    Key = "top",
                    Label = "上层",
                    Pipes = [new() { PipeTypeId = 3, Qty = 1 }]
                },
                new()
                {
                    Key = "bottom",
                    Label = "下层",
                    Pipes = [new() { PipeTypeId = 7, Qty = 1 }]
                }
            ]
        }
    ];
}

static void AssertSlotCalculationsEquivalent(
    TrunkingCalcResponse expected,
    TrunkingCalcResponse actual,
    string message)
{
    AssertEqual(expected.TotalArea, actual.TotalArea, $"{message}：总面积");
    AssertEqual(expected.TotalPipeCount, actual.TotalPipeCount, $"{message}：管线总数");
    AssertEqual(expected.Slots.Count, actual.Slots.Count, $"{message}：横向段数量");
    AssertEqual(expected.SideSlots.Count, actual.SideSlots.Count, $"{message}：竖向段数量");

    var expectedSegments = expected.Slots.Concat(expected.SideSlots).ToList();
    var actualSegments = actual.Slots.Concat(actual.SideSlots).ToList();
    for (var segmentIndex = 0; segmentIndex < expectedSegments.Count; segmentIndex++)
    {
        var expectedSegment = expectedSegments[segmentIndex];
        var actualSegment = actualSegments[segmentIndex];
        AssertEqual(expectedSegment.Name, actualSegment.Name, $"{message}：第 {segmentIndex + 1} 段名称");
        AssertEqual(expectedSegment.Sections.Count, actualSegment.Sections.Count, $"{message}：第 {segmentIndex + 1} 段分区数量");

        for (var sectionIndex = 0; sectionIndex < expectedSegment.Sections.Count; sectionIndex++)
        {
            var expectedSection = expectedSegment.Sections[sectionIndex];
            var actualSection = actualSegment.Sections[sectionIndex];
            AssertEqual(expectedSection.Key, actualSection.Key, $"{message}：第 {segmentIndex + 1} 段第 {sectionIndex + 1} 分区");
            AssertEqual(expectedSection.TotalArea, actualSection.TotalArea, $"{message}：第 {segmentIndex + 1} 段第 {sectionIndex + 1} 分区面积");
            AssertEqual(expectedSection.TotalPipeCount, actualSection.TotalPipeCount, $"{message}：第 {segmentIndex + 1} 段第 {sectionIndex + 1} 管线数");
            AssertEqual(expectedSection.SelectedTrunking?.Model, actualSection.SelectedTrunking?.Model, $"{message}：第 {segmentIndex + 1} 段第 {sectionIndex + 1} 推荐型号");
        }
    }
}

static DragChainDbContext CreateContext(string dbPath)
{
    var options = new DbContextOptionsBuilder<DragChainDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    return new DragChainDbContext(options);
}

static SensorDbContext CreateSensorContext(string dbPath)
{
    var options = new DbContextOptionsBuilder<SensorDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    return new SensorDbContext(options);
}

static void Seed(DragChainDbContext context)
{
    context.PipeTypes.AddRange(
        new PipeType { Id = 1, Name = "弱电1", Type = PipeTypeCategory.WeakCable, Diameter = 6 },
        new PipeType { Id = 2, Name = "弱电2", Type = PipeTypeCategory.WeakCable, Diameter = 6 },
        new PipeType { Id = 3, Name = "弱电3", Type = PipeTypeCategory.WeakCable, Diameter = 8 },
        new PipeType { Id = 4, Name = "弱电4", Type = PipeTypeCategory.WeakCable, Diameter = 8 },
        new PipeType { Id = 5, Name = "弱电5", Type = PipeTypeCategory.WeakCable, Diameter = 10 },
        new PipeType { Id = 6, Name = "编码器1", Type = PipeTypeCategory.Encoder, Diameter = 10 },
        new PipeType { Id = 7, Name = "强电1", Type = PipeTypeCategory.StrongCable, Diameter = 8 }
    );

    context.TrunkingCatalog.AddRange(
        new TrunkingCatalog { Id = 1, Model = "TK-25×25", Width = 25, Height = 25, CrossSection = 625, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Id = 2, Model = "TK-40×25", Width = 40, Height = 25, CrossSection = 1000, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Id = 3, Model = "TK-40×40", Width = 40, Height = 40, CrossSection = 1600, FillRatioLimit = 0.60m }
    );

    context.SaveChanges();
}

static void SeedWithoutLargestTrunking(DragChainDbContext context)
{
    var id = 1;
    foreach (var t in CatalogSeeder.GetTrunkingDefaults().Where(t => t.Model != "TK-100×100"))
    {
        context.TrunkingCatalog.Add(new TrunkingCatalog
        {
            Id = id++,
            Model = t.Model,
            Width = t.Width,
            Height = t.Height,
            CrossSection = t.CrossSection,
            FillRatioLimit = t.FillRatioLimit
        });
    }

    context.SaveChanges();
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}：期望 {expected}，实际 {actual}");
    }
}

static string? MatchApiPermission(string method, string path)
{
    var context = new DefaultHttpContext();
    context.Request.Method = method;
    context.Request.Path = path;
    return RbacPermissionCatalog.MatchApiPermission(context.Request);
}

static void ClearInMemoryAuthSessions()
{
    foreach (var fieldName in new[] { "Sessions", "RefreshSessions" })
    {
        var field = typeof(AuthController).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            ?? throw new InvalidOperationException($"找不到登录态字段 {fieldName}");
        var value = field.GetValue(null)
            ?? throw new InvalidOperationException($"登录态字段 {fieldName} 类型异常");
        var clearMethod = value.GetType().GetMethod("Clear")
            ?? throw new InvalidOperationException($"登录态字段 {fieldName} 不支持清空");
        clearMethod.Invoke(value, null);
    }
}

static byte[] SetWorkbookPassword(byte[] workbook, int rowNumber, string password)
{
    using var stream = new MemoryStream();
    stream.Write(workbook);
    stream.Position = 0;

    using (var archive = new ZipArchive(stream, ZipArchiveMode.Update, leaveOpen: true))
    {
        const string worksheetPath = "xl/worksheets/sheet1.xml";
        var entry = archive.GetEntry(worksheetPath)
            ?? throw new InvalidOperationException("测试工作簿缺少 sheet1.xml");
        string xml;
        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
        {
            xml = reader.ReadToEnd();
        }

        var emptyCell = $"<c r=\"E{rowNumber}\" t=\"inlineStr\"><is><t></t></is></c>";
        var passwordCell = $"<c r=\"E{rowNumber}\" t=\"inlineStr\"><is><t>{SecurityElement.Escape(password)}</t></is></c>";
        var updatedXml = xml.Replace(emptyCell, passwordCell, StringComparison.Ordinal);
        if (updatedXml == xml)
            throw new InvalidOperationException($"测试工作簿未找到 E{rowNumber} 密码单元格");

        entry.Delete();
        var updatedEntry = archive.CreateEntry(worksheetPath, CompressionLevel.Fastest);
        using var writer = new StreamWriter(updatedEntry.Open(), new UTF8Encoding(false));
        writer.Write(updatedXml);
    }

    return stream.ToArray();
}
