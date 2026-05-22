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

    AssertEqual(0.48m, Math.Round(relaxed.ActualFillRatio, 4), "50% 上限下实际填充率");
    AssertEqual(0.48m, Math.Round(strict.ActualFillRatio, 4), "20% 上限下实际填充率不能因为推荐线槽变更而变化");
    AssertEqual("TK-40×40", strict.WeakSide?.SelectedTrunking?.Model, "20% 上限下仍应推荐更大的线槽");

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
    AssertEqual("err", overLimit.ResultStatus, "实际填充率大于填充率上限时总览必须判定超限");
    AssertEqual("err", overLimit.WeakSide?.ResultStatus, "弱电侧实际填充率大于上限时必须判定超限");

    var slotted = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.2m,
        Slots =
        [
            new()
            {
                Id = "slot-lr",
                Name = "左右槽位A",
                Layout = "leftRight",
                LeftTrunkingId = 1,
                RightTrunkingId = 1,
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1 },
                    new() { PipeTypeId = 7, Qty = 1 },
                    new() { PipeTypeId = 6, Qty = 1 }
                ]
            },
            new()
            {
                Id = "slot-tb",
                Name = "上下槽位A",
                Layout = "topBottom",
                Sections =
                [
                    new()
                    {
                        Key = "top",
                        Label = "上层",
                        SelectedTrunkingId = 1,
                        Pipes = [new() { PipeTypeId = 1, Qty = 1 }]
                    },
                    new()
                    {
                        Key = "bottom",
                        Label = "下层",
                        SelectedTrunkingId = 1,
                        Pipes = [new() { PipeTypeId = 1, Qty = 1 }]
                    }
                ]
            }
        ]
    });

    AssertEqual(2, slotted.Slots.Count, "必须返回两个槽位结果");
    var leftRight = slotted.Slots[0];
    AssertEqual("左右槽位A", leftRight.Name, "左右槽位必须保留名称");
    AssertEqual(2, leftRight.Sections.Count, "左右槽位必须拆成左右两个分区");
    AssertEqual("left", leftRight.Sections[0].Key, "弱电和编码器必须进入左侧");
    AssertEqual(136m, leftRight.Sections[0].TotalArea, "左侧面积必须包含弱电和编码器");
    AssertEqual("right", leftRight.Sections[1].Key, "强电必须进入右侧");
    AssertEqual(64m, leftRight.Sections[1].TotalArea, "右侧面积必须只包含强电");

    var topBottom = slotted.Slots[1];
    AssertEqual(2, topBottom.Sections.Count, "上下槽位必须保留上下两个分区");
    AssertEqual(36m, topBottom.Sections[0].TotalArea, "上层必须按用户分配计算");
    AssertEqual(36m, topBottom.Sections[1].TotalArea, "同一管线允许重复放入下层并重复计算");

    var selectedStable = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.1m,
        Slots =
        [
            new()
            {
                Id = "slot-selected",
                Name = "已选线槽",
                Layout = "leftRight",
                LeftTrunkingId = 1,
                RightTrunkingId = 1,
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1 },
                    new() { PipeTypeId = 2, Qty = 1 }
                ]
            }
        ]
    });

    var selectedRelaxed = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.9m,
        Slots =
        [
            new()
            {
                Id = "slot-selected",
                Name = "已选线槽",
                Layout = "leftRight",
                LeftTrunkingId = 1,
                RightTrunkingId = 1,
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1 },
                    new() { PipeTypeId = 2, Qty = 1 }
                ]
            }
        ]
    });

    AssertEqual(0.1152m, Math.Round(selectedStable.Slots[0].Sections[0].ActualFillRatio, 4), "分区实际填充率必须按用户已选线槽计算");
    AssertEqual(0.1152m, Math.Round(selectedRelaxed.Slots[0].Sections[0].ActualFillRatio, 4), "改变填充率上限不能改变已选线槽下的实际填充率");
    AssertEqual("err", selectedStable.Slots[0].Sections[0].ResultStatus, "已选线槽实际填充率大于上限时必须超限");
    AssertEqual("ok", selectedRelaxed.Slots[0].Sections[0].ResultStatus, "上限放宽后同一实际填充率可通过");
    AssertEqual("TK-25×25", selectedStable.Slots[0].Sections[0].SelectedTrunking?.Model, "分区结果必须返回用户已选线槽");

    var sectionOverride = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.9m,
        Slots =
        [
            new()
            {
                Id = "slot-fill-override",
                Name = "单独填充率",
                Layout = "leftRight",
                LeftTrunkingId = 1,
                RightTrunkingId = 1,
                LeftFillRatio = 0.1m,
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1 },
                    new() { PipeTypeId = 2, Qty = 1 }
                ]
            }
        ]
    });

    var sectionRelaxedOverride = await service.CalculateAsync(new TrunkingCalcRequest
    {
        FillRatio = 0.1m,
        Slots =
        [
            new()
            {
                Id = "slot-fill-override",
                Name = "单独填充率",
                Layout = "leftRight",
                LeftTrunkingId = 1,
                RightTrunkingId = 1,
                LeftFillRatio = 0.9m,
                Pipes =
                [
                    new() { PipeTypeId = 1, Qty = 1 },
                    new() { PipeTypeId = 2, Qty = 1 }
                ]
            }
        ]
    });

    AssertEqual(0.1m, sectionOverride.Slots[0].Sections[0].FillRatio, "左侧分区必须使用单独填充率上限");
    AssertEqual("err", sectionOverride.Slots[0].Sections[0].ResultStatus, "单独上限更严格时必须按单独上限判定");
    AssertEqual(0.9m, sectionRelaxedOverride.Slots[0].Sections[0].FillRatio, "单独填充率应覆盖全局上限");
    AssertEqual("ok", sectionRelaxedOverride.Slots[0].Sections[0].ResultStatus, "单独上限更宽松时必须按单独上限判定");
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
        new TrunkingCatalog { Id = 1, Model = "TK-25×25", Width = 25, Height = 25, CrossSection = 625 },
        new TrunkingCatalog { Id = 2, Model = "TK-40×25", Width = 40, Height = 25, CrossSection = 1000 },
        new TrunkingCatalog { Id = 3, Model = "TK-40×40", Width = 40, Height = 40, CrossSection = 1600 }
    );

    context.SaveChanges();
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}：期望 {expected}，实际 {actual}");
    }
}
