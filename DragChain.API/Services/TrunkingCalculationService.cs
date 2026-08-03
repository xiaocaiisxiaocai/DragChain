using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;

namespace DragChain.API.Services;

public interface ITrunkingCalculationService
{
    Task<TrunkingCalcResponse> CalculateAsync(TrunkingCalcRequest request);
}

public class TrunkingCalculationService : ITrunkingCalculationService
{
    private readonly DragChainDbContext _context;
    private const decimal DEFAULT_FILL_RATIO = 0.60m;

    private class TrunkingSegmentRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<PipeItemDto> Pipes { get; set; } = new();
    }

    public TrunkingCalculationService(DragChainDbContext context)
    {
        _context = context;
    }

    public async Task<TrunkingCalcResponse> CalculateAsync(TrunkingCalcRequest request)
    {
        var allPipeTypes = await _context.PipeTypes.ToListAsync();
        var pipeMap = allPipeTypes.ToDictionary(p => p.Id);

        decimal fillRatio = request.FillRatio > 0 && request.FillRatio <= 1
            ? request.FillRatio
            : DEFAULT_FILL_RATIO;

        var trunkingList = (await _context.TrunkingCatalog.ToListAsync())
            .OrderBy(t => t.CrossSection)
            .ThenBy(t => t.Id)
            .ToList();

        if (request.Slots.Count > 0)
        {
            return CalculateSlots(request, pipeMap, trunkingList, fillRatio);
        }

        var activePipes = ResolvePipeItems(request.Pipes, pipeMap).ToList();

        var weakItems = activePipes
            .Where(item => !PipeTypeCategory.IsStrongCable(item.Pipe!.Type))
            .Select(item => (Pipe: item.Pipe!, item.Qty))
            .ToList();
        var strongItems = activePipes
            .Where(item => PipeTypeCategory.IsStrongCable(item.Pipe!.Type))
            .Select(item => (Pipe: item.Pipe!, item.Qty))
            .ToList();

        var weakSide = CalculateSide("weak", "弱电线槽", weakItems, trunkingList, fillRatio);
        var strongSide = CalculateSide("strong", "强电线槽", strongItems, trunkingList, fillRatio);
        var totalArea = weakSide.TotalArea + strongSide.TotalArea;
        var maxDia = Math.Max(weakSide.MaxPipeDia, strongSide.MaxPipeDia);
        var totalCount = weakSide.TotalPipeCount + strongSide.TotalPipeCount;

        var selectedTrunking = weakSide.SelectedTrunking ?? strongSide.SelectedTrunking;
        var actualFillRatio = Math.Max(weakSide.ActualFillRatio, strongSide.ActualFillRatio);
        var okFill = weakSide.ResultStatus != "err" && strongSide.ResultStatus != "err";

        var steps = new TrunkingStepsDto
        {
            Step1_TotalArea = $"{totalArea:F2} mm²",
            Step1_MaxDia = $"{maxDia:F1} mm",
            Step1_PipeCount = $"{totalCount} 根",
            Step2_FillRatio = "按线槽型号上限",
        };

        if (!trunkingList.Any())
        {
            steps.Step2_TrunkingArea = "—";
            steps.Step3_Result = "線槽型錄為空";
            return new TrunkingCalcResponse
            {
                TotalArea = totalArea,
                FillRatio = fillRatio,
                ActualFillRatio = 0,
                MaxPipeDia = maxDia,
                TotalPipeCount = totalCount,
                MatchResults = weakSide.MatchResults,
                WeakSide = weakSide,
                StrongSide = strongSide,
                ResultStatus = "err",
                ResultMessage = "線槽型錄為空",
                Steps = steps
            };
        }

        steps.Step2_TrunkingArea = $"弱电 {weakSide.SelectedTrunking?.CrossSection.ToString("F2") ?? "—"} / 强电 {strongSide.SelectedTrunking?.CrossSection.ToString("F2") ?? "—"} mm²";
        steps.Step3_Result = okFill
            ? $"可容納，弱电 {weakSide.ActualFillRatio * 100:F1}% / 强电 {strongSide.ActualFillRatio * 100:F1}%"
            : "存在超出线槽型号有效利用率上限的线槽";

        return new TrunkingCalcResponse
        {
            TotalArea = totalArea,
            FillRatio = fillRatio,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = selectedTrunking,
            MatchResults = weakSide.MatchResults,
            WeakSide = weakSide,
            StrongSide = strongSide,
            ResultStatus = okFill ? "ok" : "err",
            ResultMessage = okFill ? "可容納" : "强弱电线槽存在超出限制",
            Steps = steps
        };
    }

    private static TrunkingSideResultDto CalculateSide(
        string key,
        string label,
        List<(PipeType Pipe, int Qty)> pipes,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio,
        int? selectedTrunkingId = null,
        bool requireSelectedTrunking = false)
    {
        decimal totalArea = 0;
        decimal maxDia = 0;
        int totalCount = 0;

        foreach (var item in pipes)
        {
            totalArea += item.Qty * item.Pipe.Diameter * item.Pipe.Diameter;
            if (item.Pipe.Diameter > maxDia) maxDia = item.Pipe.Diameter;
            totalCount += item.Qty;
        }

        var recommendedTrunking = totalArea > 0
            ? trunkingList.FirstOrDefault(t => IsTrunkingUsable(totalArea, t)) ?? trunkingList.LastOrDefault()
            : null;
        var chosenTrunking = selectedTrunkingId.HasValue
            ? trunkingList.FirstOrDefault(t => t.Id == selectedTrunkingId.Value)
            : null;
        var baseTrunking = totalArea > 0
            ? chosenTrunking
                ?? recommendedTrunking
                ?? trunkingList.LastOrDefault()
            : null;
        var displayTrunking = requireSelectedTrunking
            ? chosenTrunking ?? recommendedTrunking
            : chosenTrunking ?? recommendedTrunking;
        var recommendedId = recommendedTrunking?.Id ?? 0;
        var matchResults = trunkingList
            .Select(t =>
            {
                var ratio = t.CrossSection > 0 ? totalArea / t.CrossSection : 0;
                var fillRatioLimit = NormalizeCatalogFillRatio(t.FillRatioLimit);
                var ok = ratio <= fillRatioLimit;
                return new TrunkingMatchResultDto
                {
                    Id = t.Id,
                    Model = t.Model,
                    Width = t.Width,
                    Height = t.Height,
                    CrossSection = t.CrossSection,
                    FillRatioLimit = fillRatioLimit,
                    ActualFillRatio = ratio,
                    OkFill = ok,
                    IsRecommended = t.Id == recommendedId,
                    Result = ok ? "可用" : "不可用"
                };
            })
            .ToList();

        var actualFillRatio = baseTrunking != null ? totalArea / baseTrunking.CrossSection : 0;
        var selectedFillRatio = baseTrunking != null ? NormalizeCatalogFillRatio(baseTrunking.FillRatioLimit) : fillRatio;
        var resultStatus = totalArea <= 0 || actualFillRatio <= selectedFillRatio ? "ok" : "err";
        var resultMessage = totalArea <= 0
            ? "无管线"
            : resultStatus == "ok"
                ? "可容纳"
                : $"有效利用率 {actualFillRatio * 100:F1}%，超出 {selectedFillRatio * 100:F0}% 限制";

        return new TrunkingSideResultDto
        {
            Key = key,
            Label = label,
            TotalArea = totalArea,
            FillRatio = selectedFillRatio,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = displayTrunking != null ? MapToDto(displayTrunking) : null,
            MatchResults = matchResults,
            ResultStatus = resultStatus,
            ResultMessage = resultMessage
        };
    }

    private static TrunkingCalcResponse CalculateSlots(
        TrunkingCalcRequest request,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var slotResults = BuildOrderedTrunkingSegments(request.Slots, request.SlotOrder)
            .Select(segment => CalculateSegment(segment, pipeMap, trunkingList, fillRatio))
            .ToList();
        var sideSlotResults = BuildSlotSideSegments(request.Slots)
            .Select(segment => CalculateSegment(segment, pipeMap, trunkingList, fillRatio))
            .ToList();

        var horizontalSections = slotResults.SelectMany(slot => slot.Sections).ToList();
        var allSections = horizontalSections.Concat(sideSlotResults.SelectMany(slot => slot.Sections)).ToList();
        var totalArea = horizontalSections.Sum(section => section.TotalArea);
        var actualFillRatio = allSections.Count > 0 ? allSections.Max(section => section.ActualFillRatio) : 0;
        var maxDia = horizontalSections.Count > 0 ? horizontalSections.Max(section => section.MaxPipeDia) : 0;
        var totalCount = horizontalSections.Sum(section => section.TotalPipeCount);
        var okFill = allSections.All(section => section.ResultStatus != "err");
        var hasWarn = allSections.Any(section => section.ResultStatus == "warn");
        var selectedTrunking = allSections.Select(section => section.SelectedTrunking).FirstOrDefault(t => t != null);

        return new TrunkingCalcResponse
        {
            TotalArea = totalArea,
            FillRatio = fillRatio,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = selectedTrunking,
            MatchResults = allSections.FirstOrDefault()?.MatchResults ?? new(),
            Slots = slotResults,
            SideSlots = sideSlotResults,
            ResultStatus = !okFill ? "err" : hasWarn ? "warn" : "ok",
            ResultMessage = !okFill ? "存在槽位超出限制" : hasWarn ? "存在槽位未选择线槽" : "可容纳",
            Steps = new TrunkingStepsDto
            {
                Step1_TotalArea = $"{totalArea:F2} mm²",
                Step1_MaxDia = $"{maxDia:F1} mm",
                Step1_PipeCount = $"{totalCount} 根",
                Step2_FillRatio = "按线槽型号上限",
                Step2_TrunkingArea = "按槽位上下相邻线槽独立计算",
                Step3_Result = !okFill
                    ? "存在超出线槽型号有效利用率上限的槽位"
                    : hasWarn ? "存在槽位未选择线槽" : "所有槽位可容纳"
            }
        };
    }

    private static TrunkingSlotResultDto CalculateSegment(
        TrunkingSegmentRequest segment,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var sections = CalculateLeftRightSections(segment, pipeMap, trunkingList, fillRatio);
        var resultStatus = sections.Any(section => section.ResultStatus == "err")
            ? "err"
            : sections.Any(section => section.ResultStatus == "warn") ? "warn" : "ok";

        return new TrunkingSlotResultDto
        {
            Id = segment.Id,
            Name = segment.Name,
            Layout = "ordered",
            Sections = sections,
            ResultStatus = resultStatus,
            ResultMessage = resultStatus == "ok" ? "可容纳" : resultStatus == "warn" ? "请选择线槽" : "存在超出限制"
        };
    }

    private static List<TrunkingSideResultDto> CalculateLeftRightSections(
        TrunkingSegmentRequest segment,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var items = ResolvePipeItems(segment.Pipes, pipeMap).ToList();
        var leftItems = items
            .Where(item => !PipeTypeCategory.IsStrongCable(item.Pipe.Type))
            .ToList();
        var rightItems = items
            .Where(item => PipeTypeCategory.IsStrongCable(item.Pipe.Type))
            .ToList();

        return new List<TrunkingSideResultDto>
        {
            CalculateSide($"{segment.Id}-left", "左侧弱电线槽", leftItems, trunkingList, fillRatio, null, true),
            CalculateSide($"{segment.Id}-right", "右侧强电线槽", rightItems, trunkingList, fillRatio, null, true)
        };
    }

    private static List<TrunkingSegmentRequest> BuildOrderedTrunkingSegments(
        List<TrunkingSlotRequestDto> slots,
        string? slotOrder)
    {
        // 旧请求没有 SlotOrder，保持其原始顺序；仅新契约明确声明 bottomToTop 时反转。
        var activeSlots = slots
            .Where(slot => !string.IsNullOrWhiteSpace(slot.Name) || slot.Pipes.Count > 0 || slot.Sections.Count > 0)
            .ToList();
        var orderedSlots = string.Equals(slotOrder, "bottomToTop", StringComparison.Ordinal)
            ? activeSlots.AsEnumerable().Reverse().ToList()
            : activeSlots;
        var segments = new List<TrunkingSegmentRequest>();
        if (orderedSlots.Count == 0) return segments;

        for (var index = 0; index <= orderedSlots.Count; index++)
        {
            var pipes = new List<PipeItemDto>();
            string name;

            if (index == 0)
            {
                var first = orderedSlots[0];
                pipes.AddRange(GetSlotLayerPipes(first, "top"));
                name = $"{GetSlotName(first, 1)}上";
            }
            else if (index == orderedSlots.Count)
            {
                var last = orderedSlots[^1];
                pipes.AddRange(GetSlotLayerPipes(last, "bottom"));
                name = $"{GetSlotName(last, orderedSlots.Count)}下";
            }
            else
            {
                var upper = orderedSlots[index - 1];
                var lower = orderedSlots[index];
                pipes.AddRange(GetSlotLayerPipes(upper, "bottom"));
                pipes.AddRange(GetSlotLayerPipes(lower, "top"));
                name = $"{GetSlotName(upper, index)}下 + {GetSlotName(lower, index + 1)}上";
            }

            segments.Add(new TrunkingSegmentRequest
            {
                Id = $"segment-{index + 1}",
                Name = name,
                Pipes = MergePipeItems(pipes)
            });
        }

        return segments;
    }

    private static List<TrunkingSegmentRequest> BuildSlotSideSegments(List<TrunkingSlotRequestDto> slots)
    {
        var orderedSlots = slots
            .Where(slot => !string.IsNullOrWhiteSpace(slot.Name) || slot.Pipes.Count > 0 || slot.Sections.Count > 0)
            .ToList();

        if (orderedSlots.Count == 0) return new List<TrunkingSegmentRequest>();

        // 右侧方案中，左右侧边线槽是贯通竖槽：左侧汇总弱电，右侧汇总强电。
        var sidePipes = orderedSlots
            .SelectMany(slot => GetSlotLayerPipes(slot, "top")
                .Concat(GetSlotLayerPipes(slot, "bottom")));

        return new List<TrunkingSegmentRequest>
        {
            new()
            {
                Id = "slot-side-vertical",
                Name = "左右竖向线槽",
                Pipes = MergePipeItems(sidePipes)
            }
        };
    }

    private static List<PipeItemDto> GetSlotLayerPipes(TrunkingSlotRequestDto slot, string layer)
    {
        var directPipes = slot.Pipes
            .Where(pipe => NormalizeLayer(pipe.Layer) == layer)
            .Select(pipe => new PipeItemDto { PipeTypeId = pipe.PipeTypeId, Qty = pipe.Qty, Layer = layer });
        var sectionPipes = slot.Sections
            .Where(section => NormalizeLayer(section.Key) == layer)
            .SelectMany(section => section.Pipes)
            .Select(pipe => new PipeItemDto { PipeTypeId = pipe.PipeTypeId, Qty = pipe.Qty, Layer = layer });

        return directPipes.Concat(sectionPipes).ToList();
    }

    private static List<PipeItemDto> MergePipeItems(IEnumerable<PipeItemDto> pipes)
    {
        return pipes
            .Where(pipe => pipe.PipeTypeId > 0 && pipe.Qty > 0)
            .GroupBy(pipe => pipe.PipeTypeId)
            .Select(group => new PipeItemDto
            {
                PipeTypeId = group.Key,
                Qty = group.Sum(pipe => pipe.Qty),
                Layer = "top"
            })
            .ToList();
    }

    private static string GetSlotName(TrunkingSlotRequestDto slot, int index)
    {
        return string.IsNullOrWhiteSpace(slot.Name) ? $"槽位{index}" : slot.Name.Trim();
    }

    private static string NormalizeLayer(string? layer) => layer == "bottom" ? "bottom" : "top";

    private static bool IsTrunkingUsable(decimal totalArea, TrunkingCatalog trunking)
    {
        if (trunking.CrossSection <= 0) return false;
        return totalArea / trunking.CrossSection <= NormalizeCatalogFillRatio(trunking.FillRatioLimit);
    }

    private static decimal NormalizeCatalogFillRatio(decimal fillRatio)
    {
        return fillRatio > 0 && fillRatio <= 1 ? fillRatio : DEFAULT_FILL_RATIO;
    }

    private static IEnumerable<(PipeType Pipe, int Qty)> ResolvePipeItems(
        IEnumerable<PipeItemDto> items,
        Dictionary<int, PipeType> pipeMap)
    {
        return items
            .Where(p => p.PipeTypeId > 0 && p.Qty > 0)
            .Select(p => (Pipe: pipeMap.GetValueOrDefault(p.PipeTypeId), p.Qty))
            .Where(item => item.Pipe != null)
            .Where(item => item.Pipe!.Type != PipeTypeCategory.Tube)
            .Select(item => (Pipe: item.Pipe!, item.Qty));
    }

    private static TrunkingCatalogDto MapToDto(TrunkingCatalog t) => new()
    {
        Id = t.Id,
        Model = t.Model,
        Width = t.Width,
        Height = t.Height,
        CrossSection = t.CrossSection,
        FillRatioLimit = NormalizeCatalogFillRatio(t.FillRatioLimit)
    };
}
