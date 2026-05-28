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
            Step2_FillRatio = $"{fillRatio * 100:F0} %",
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
            : $"存在超出容納能力的线槽（有效利用率建议 ≤{fillRatio * 100:F0}%）";

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

        var requiredArea = fillRatio > 0 ? totalArea / fillRatio : totalArea;
        var recommendedTrunking = totalArea > 0
            ? trunkingList.FirstOrDefault(t => t.CrossSection >= requiredArea) ?? trunkingList.LastOrDefault()
            : null;
        var chosenTrunking = selectedTrunkingId.HasValue
            ? trunkingList.FirstOrDefault(t => t.Id == selectedTrunkingId.Value)
            : null;
        var baseTrunking = totalArea > 0
            ? chosenTrunking
                ?? (requireSelectedTrunking
                    ? recommendedTrunking
                    : trunkingList.FirstOrDefault(t => t.CrossSection >= totalArea))
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
                var ok = ratio <= fillRatio;
                return new TrunkingMatchResultDto
                {
                    Id = t.Id,
                    Model = t.Model,
                    Width = t.Width,
                    Height = t.Height,
                    CrossSection = t.CrossSection,
                    ActualFillRatio = ratio,
                    OkFill = ok,
                    IsRecommended = t.Id == recommendedId,
                    Result = ok ? "可用" : "不可用"
                };
            })
            .ToList();

        var actualFillRatio = baseTrunking != null ? totalArea / baseTrunking.CrossSection : 0;
        var resultStatus = totalArea <= 0 || actualFillRatio <= fillRatio ? "ok" : "err";
        var resultMessage = totalArea <= 0
            ? "无管线"
            : resultStatus == "ok"
                ? "可容纳"
                : $"有效利用率 {actualFillRatio * 100:F1}%，超出 {fillRatio * 100:F0}% 限制";

        return new TrunkingSideResultDto
        {
            Key = key,
            Label = label,
            TotalArea = totalArea,
            FillRatio = fillRatio,
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
        var slotResults = request.Slots
            .Select(slot => CalculateSlot(slot, pipeMap, trunkingList, fillRatio))
            .ToList();

        var sections = slotResults.SelectMany(slot => slot.Sections).ToList();
        var totalArea = sections.Sum(section => section.TotalArea);
        var actualFillRatio = sections.Count > 0 ? sections.Max(section => section.ActualFillRatio) : 0;
        var maxDia = sections.Count > 0 ? sections.Max(section => section.MaxPipeDia) : 0;
        var totalCount = sections.Sum(section => section.TotalPipeCount);
        var okFill = sections.All(section => section.ResultStatus != "err");
        var hasWarn = sections.Any(section => section.ResultStatus == "warn");
        var selectedTrunking = sections.Select(section => section.SelectedTrunking).FirstOrDefault(t => t != null);

        return new TrunkingCalcResponse
        {
            TotalArea = totalArea,
            FillRatio = fillRatio,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = selectedTrunking,
            MatchResults = sections.FirstOrDefault()?.MatchResults ?? new(),
            Slots = slotResults,
            ResultStatus = !okFill ? "err" : hasWarn ? "warn" : "ok",
            ResultMessage = !okFill ? "存在槽位超出限制" : hasWarn ? "存在槽位未选择线槽" : "可容纳",
            Steps = new TrunkingStepsDto
            {
                Step1_TotalArea = $"{totalArea:F2} mm²",
                Step1_MaxDia = $"{maxDia:F1} mm",
                Step1_PipeCount = $"{totalCount} 根",
                Step2_FillRatio = $"{fillRatio * 100:F0} %",
                Step2_TrunkingArea = "按槽位分区独立计算",
                Step3_Result = !okFill
                    ? $"存在超出容纳能力的槽位（有效利用率建议 ≤{fillRatio * 100:F0}%）"
                    : hasWarn ? "存在槽位未选择线槽" : "所有槽位可容纳"
            }
        };
    }

    private static TrunkingSlotResultDto CalculateSlot(
        TrunkingSlotRequestDto slot,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var layout = slot.Layout == "topBottom" ? "topBottom" : "leftRight";
        var sections = layout == "topBottom"
            ? CalculateTopBottomSections(slot, pipeMap, trunkingList, fillRatio)
            : CalculateLeftRightSections(slot, pipeMap, trunkingList, fillRatio);
        var resultStatus = sections.Any(section => section.ResultStatus == "err")
            ? "err"
            : sections.Any(section => section.ResultStatus == "warn") ? "warn" : "ok";

        return new TrunkingSlotResultDto
        {
            Id = slot.Id,
            Name = string.IsNullOrWhiteSpace(slot.Name) ? "未命名槽位" : slot.Name.Trim(),
            Layout = layout,
            Sections = sections,
            ResultStatus = resultStatus,
            ResultMessage = resultStatus == "ok" ? "可容纳" : resultStatus == "warn" ? "请选择线槽" : "存在超出限制"
        };
    }

    private static List<TrunkingSideResultDto> CalculateLeftRightSections(
        TrunkingSlotRequestDto slot,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var items = ResolvePipeItems(slot.Pipes, pipeMap).ToList();
        var leftItems = items
            .Where(item => !PipeTypeCategory.IsStrongCable(item.Pipe.Type))
            .ToList();
        var rightItems = items
            .Where(item => PipeTypeCategory.IsStrongCable(item.Pipe.Type))
            .ToList();

        return new List<TrunkingSideResultDto>
        {
            CalculateSide("left", "左侧弱电线槽", leftItems, trunkingList, NormalizeFillRatio(slot.LeftFillRatio, fillRatio), slot.LeftTrunkingId, true),
            CalculateSide("right", "右侧强电线槽", rightItems, trunkingList, NormalizeFillRatio(slot.RightFillRatio, fillRatio), slot.RightTrunkingId, true)
        };
    }

    private static List<TrunkingSideResultDto> CalculateTopBottomSections(
        TrunkingSlotRequestDto slot,
        Dictionary<int, PipeType> pipeMap,
        List<TrunkingCatalog> trunkingList,
        decimal fillRatio)
    {
        var top = slot.Sections.FirstOrDefault(section => section.Key == "top");
        var bottom = slot.Sections.FirstOrDefault(section => section.Key == "bottom");

        return new List<TrunkingSideResultDto>
        {
            CalculateSide("top", "上层线槽", ResolvePipeItems(top?.Pipes ?? new(), pipeMap).ToList(), trunkingList, NormalizeFillRatio(top?.FillRatio, fillRatio), top?.SelectedTrunkingId, true),
            CalculateSide("bottom", "下层线槽", ResolvePipeItems(bottom?.Pipes ?? new(), pipeMap).ToList(), trunkingList, NormalizeFillRatio(bottom?.FillRatio, fillRatio), bottom?.SelectedTrunkingId, true)
        };
    }

    private static decimal NormalizeFillRatio(decimal? sectionFillRatio, decimal fallback)
    {
        return sectionFillRatio.HasValue && sectionFillRatio.Value > 0 && sectionFillRatio.Value <= 1
            ? sectionFillRatio.Value
            : fallback;
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
        CrossSection = t.CrossSection
    };
}
