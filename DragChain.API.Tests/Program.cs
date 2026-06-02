using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;
using Microsoft.EntityFrameworkCore;

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

    var topDownSlots = await service.CalculateAsync(new TrunkingCalcRequest
    {
        Slots =
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
        ]
    });

    AssertEqual(3, topDownSlots.Slots.Count, "两个槽位从上到下必须生成三条线槽段");
    AssertEqual(2, topDownSlots.SideSlots.Count, "两个槽位必须生成两组左右线槽结果");
    AssertEqual("槽位1上", topDownSlots.Slots[0].Name, "第一条线槽段必须是槽位1上");
    AssertEqual("槽位1下 + 槽位2上", topDownSlots.Slots[1].Name, "中间线槽段必须合并上槽位下和下槽位上");
    AssertEqual("槽位2下", topDownSlots.Slots[2].Name, "最后一条线槽段必须是槽位2下");
    AssertEqual(36m, topDownSlots.Slots[0].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位1上左侧必须包含弱电");
    AssertEqual(64m, topDownSlots.Slots[0].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位1上右侧必须包含强电");
    AssertEqual(100m, topDownSlots.Slots[1].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "中间段左侧必须包含槽位1下加槽位2上的弱电");
    AssertEqual(0m, topDownSlots.Slots[1].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "中间段右侧无强电时必须为 0");
    AssertEqual(0m, topDownSlots.Slots[2].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位2下左侧无弱电时必须为 0");
    AssertEqual(64m, topDownSlots.Slots[2].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位2下右侧必须包含强电");
    AssertEqual("槽位1", topDownSlots.SideSlots[0].Name, "第一组左右线槽结果必须对应槽位1");
    AssertEqual(72m, topDownSlots.SideSlots[0].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位1左侧必须包含上下层弱电");
    AssertEqual(64m, topDownSlots.SideSlots[0].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位1右侧必须包含上下层强电");
    AssertEqual("槽位2", topDownSlots.SideSlots[1].Name, "第二组左右线槽结果必须对应槽位2");
    AssertEqual(64m, topDownSlots.SideSlots[1].Sections.Single(section => section.Key.EndsWith("-left")).TotalArea, "槽位2左侧必须包含上下层弱电");
    AssertEqual(64m, topDownSlots.SideSlots[1].Sections.Single(section => section.Key.EndsWith("-right")).TotalArea, "槽位2右侧必须包含上下层强电");
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

static DragChainDbContext CreateContext(string dbPath)
{
    var options = new DbContextOptionsBuilder<DragChainDbContext>()
        .UseSqlite($"Data Source={dbPath}")
        .Options;

    return new DragChainDbContext(options);
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
