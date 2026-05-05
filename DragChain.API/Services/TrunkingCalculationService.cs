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
            .ToList();

        decimal totalArea = 0;
        decimal maxDia = 0;
        int totalCount = 0;

        foreach (var ap in activePipes)
        {
            var p = ap.Pipe!;
            if (ap.Qty > 0)
            {
                totalArea += ap.Qty * (decimal)(Math.PI * Math.Pow((double)(p.Diameter / 2), 2));
                if (p.Diameter > maxDia) maxDia = p.Diameter;
                totalCount += ap.Qty;
            }
        }

        decimal fillRatio = request.FillRatio > 0 && request.FillRatio <= 1
            ? request.FillRatio
            : DEFAULT_FILL_RATIO;

        var steps = new TrunkingStepsDto
        {
            Step1_TotalArea = $"{totalArea:F2} mm²",
            Step1_MaxDia = $"{maxDia:F1} mm",
            Step1_PipeCount = $"{totalCount} 根",
            Step2_FillRatio = $"{fillRatio * 100:F0} %",
        };

        if (request.SelectedTrunkingId <= 0)
        {
            steps.Step2_TrunkingArea = "—";
            steps.Step3_Result = "請選擇線槽型號";
            return new TrunkingCalcResponse
            {
                TotalArea = totalArea,
                FillRatio = fillRatio,
                ActualFillRatio = 0,
                MaxPipeDia = maxDia,
                TotalPipeCount = totalCount,
                ResultStatus = "warn",
                ResultMessage = "請選擇線槽",
                Steps = steps
            };
        }

        var selectedTrunking = await _context.TrunkingCatalog.FindAsync(request.SelectedTrunkingId);
        if (selectedTrunking == null)
        {
            steps.Step2_TrunkingArea = "—";
            steps.Step3_Result = "選定的線槽不存在";
            return new TrunkingCalcResponse
            {
                TotalArea = totalArea,
                FillRatio = fillRatio,
                ActualFillRatio = 0,
                MaxPipeDia = maxDia,
                TotalPipeCount = totalCount,
                ResultStatus = "err",
                ResultMessage = "線槽不存在",
                Steps = steps
            };
        }

        decimal actualFillRatio = totalArea / selectedTrunking.CrossSection;
        bool okFill = actualFillRatio <= fillRatio;

        steps.Step2_TrunkingArea = $"{selectedTrunking.CrossSection:F2} mm²";
        steps.Step3_Result = okFill
            ? $"可容納，填充率 {actualFillRatio * 100:F1}%"
            : $"超出容納能力，填充率 {actualFillRatio * 100:F1}%（建議 ≤{fillRatio * 100:F0}%）";

        return new TrunkingCalcResponse
        {
            TotalArea = totalArea,
            FillRatio = fillRatio,
            ActualFillRatio = actualFillRatio,
            MaxPipeDia = maxDia,
            TotalPipeCount = totalCount,
            SelectedTrunking = MapToDto(selectedTrunking),
            ResultStatus = okFill ? "ok" : "err",
            ResultMessage = okFill ? "可容納" : $"填充率 {actualFillRatio * 100:F1}%，超出 {fillRatio * 100:F0}% 限制",
            Steps = steps
        };
    }

    private static TrunkingCatalogDto MapToDto(TrunkingCatalog t) => new()
    {
        Id = t.Id,
        Model = t.Model,
        Width = t.Width,
        Height = t.Height,
        InnerWidth = t.InnerWidth,
        InnerHeight = t.InnerHeight,
        CrossSection = t.CrossSection,
        Material = t.Material,
        Remarks = t.Remarks
    };
}
