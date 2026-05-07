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
    private const decimal DEFAULT_FILL_RATIO = 0.75m;

    public TrunkingCalculationService(DragChainDbContext context)
    {
        _context = context;
    }

    public async Task<TrunkingCalcResponse> CalculateAsync(TrunkingCalcRequest request)
    {
        var allPipeTypes = await _context.PipeTypes.ToListAsync();
        var pipeMap = allPipeTypes.ToDictionary(p => p.Id);

        var activePipes = request.Pipes
            .Where(p => p.PipeTypeId > 0 && p.Qty > 0)
            .Select(p => new
            {
                Pipe = pipeMap.GetValueOrDefault(p.PipeTypeId),
                p.Qty
            })
            .Where(x => x.Pipe != null)
            .Where(x => x.Pipe!.Type != PipeTypeCategory.Tube)
            .ToList();

        decimal fillRatio = request.FillRatio > 0 && request.FillRatio <= 1
            ? request.FillRatio
            : DEFAULT_FILL_RATIO;

        var trunkingList = (await _context.TrunkingCatalog.ToListAsync())
            .OrderBy(t => t.CrossSection)
            .ThenBy(t => t.Id)
            .ToList();

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
            : $"存在超出容納能力的线槽（建议 ≤{fillRatio * 100:F0}%）";

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
        decimal fillRatio)
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
        var selectedTrunking = totalArea > 0
            ? trunkingList.FirstOrDefault(t => t.CrossSection >= requiredArea) ?? trunkingList.LastOrDefault()
            : null;
        var recommendedId = selectedTrunking?.Id ?? 0;
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

        var actualFillRatio = selectedTrunking != null ? totalArea / selectedTrunking.CrossSection : 0;
        var resultStatus = totalArea <= 0 || actualFillRatio <= fillRatio ? "ok" : "err";
        var resultMessage = totalArea <= 0
            ? "无管线"
            : resultStatus == "ok"
                ? "可容纳"
                : $"填充率 {actualFillRatio * 100:F1}%，超出 {fillRatio * 100:F0}% 限制";

        return new TrunkingSideResultDto
        {
            Key = key,
            Label = label,
            TotalArea = totalArea,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = selectedTrunking != null ? MapToDto(selectedTrunking) : null,
            MatchResults = matchResults,
            ResultStatus = resultStatus,
            ResultMessage = resultMessage
        };
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
