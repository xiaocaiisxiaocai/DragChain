using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;

namespace DragChain.API.Services;

public interface ICalculationService
{
    Task<CalculationResponse> CalculateAsync(CalculationRequest request);
}

public class CalculationService : ICalculationService
{
    private readonly DragChainDbContext _context;

    public CalculationService(DragChainDbContext context)
    {
        _context = context;
    }

    public async Task<CalculationResponse> CalculateAsync(CalculationRequest request)
    {
        // 加载管线类型数据
        var allPipeTypes = await _context.PipeTypes.ToListAsync();
        var pipeMap = allPipeTypes.ToDictionary(p => p.Id);

        // 解析管线列表
        var activePipes = request.Pipes
            .Where(p => p.PipeTypeId > 0 && p.Qty > 0)
            .Select(p => new
            {
                Pipe = pipeMap.GetValueOrDefault(p.PipeTypeId),
                p.Qty
            })
            .Where(x => x.Pipe != null)
            .ToList();

        var hasAnyPipe = activePipes.Count > 0;

        // 电缆芯数计算（Excel 公式）
        int cores = request.SensorCount + (int)Math.Ceiling(request.SensorCount / 3.0) * 2
                   + (int)Math.Ceiling(request.MagnetCount / 3.0) * 2 + 2;

        // 占空比
        decimal ratio = request.Brand.ToLower() == "wzl" ? 0.60m : 0.55m;

        // 管线汇总统计
        decimal totalWeight = 0;
        decimal totalArea = 0;
        decimal maxDia = 0;
        decimal tubeBend = 0, cableBend = 0, encoderBend = 0;

        foreach (var ap in activePipes)
        {
            var p = ap.Pipe!;
            if (ap.Qty > 0)
            {
                totalWeight += ap.Qty * p.Weight;
                totalArea += ap.Qty * (decimal)(Math.PI * Math.Pow((double)(p.Diameter / 2), 2));
                if (p.Diameter > maxDia) maxDia = p.Diameter;

                decimal b = p.Diameter * p.BendMultiplier;
                switch (p.Type)
                {
                    case "tube":
                        if (b > tubeBend) tubeBend = b;
                        break;
                    case "encoder":
                        if (b > encoderBend) encoderBend = b;
                        break;
                    default: // cable / other
                        if (b > cableBend) cableBend = b;
                        break;
                }
            }
        }

        decimal maxBend = Math.Max(Math.Max(tubeBend, cableBend), encoderBend);
        decimal minHeight = maxDia * 1.25m;
        decimal minArea = totalArea / ratio;
        decimal needSpan = request.MotionType == "横移" ? request.Stroke / 2.0m : 0;

        // 空管线清单 -> 快速返回
        if (!hasAnyPipe)
        {
            return new CalculationResponse
            {
                MinHeight = minHeight,
                MinRadius = maxBend,
                TotalArea = totalArea,
                MinInnerArea = minArea,
                TotalWeight = totalWeight,
                NeedSpan = needSpan,
                CoreCount = cores,
                TubeBend = tubeBend,
                CableBend = cableBend,
                EncoderBend = encoderBend,
                ResultStatus = "warn",
                ResultMessage = "請填寫管線清單",
                Steps = BuildEmptySteps()
            };
        }

        // 根据品牌执行计算
        if (request.Brand.ToLower() == "wzl")
            return await CalcWzlAsync(request, minHeight, maxBend, minArea, totalArea, totalWeight, needSpan, cores, tubeBend, cableBend, encoderBend, ratio);
        else
            return await CalcMeAsync(request, minHeight, maxBend, minArea, totalArea, totalWeight, needSpan, cores, tubeBend, cableBend, encoderBend, ratio);
    }

    private async Task<CalculationResponse> CalcWzlAsync(
        CalculationRequest request, decimal minHeight, decimal maxBend, decimal minArea,
        decimal totalArea, decimal weight, decimal needSpan, int cores,
        decimal tubeBend, decimal cableBend, decimal encoderBend, decimal ratio)
    {
        // 加载 WZL 选型模型（S:標準款 行，area 不为 null）
        var wzlModels = await _context.WzlCatalog
            .Where(w => w.Function == "S:標準款" && w.InnerArea != null)
            .ToListAsync();

        var wzlSpanData = CatalogSeeder.GetDefaults().wzlSpanData;

        var matchResults = new List<MatchResultDto>();
        WzlCatalog? prelimModel = null;
        WzlCatalog? finalModel = null;

        foreach (var m in wzlModels)
        {
            bool okH = minHeight <= m.InnerHeight;
            bool okR = maxBend <= m.RecRadius;
            bool okA = minArea <= (m.InnerArea ?? 0);
            bool okPrelim = okH && okR && okA;
            if (okPrelim && prelimModel == null) prelimModel = m;

            // 架空能力
            var spanData = wzlSpanData.GetValueOrDefault(m.Model);
            decimal maxW = spanData.maxW;
            decimal spanBase = spanData.spanBase;
            decimal spanSlope = spanData.spanSlope;
            decimal calcSpan = (weight > maxW) ? 0 : Math.Max(0, spanBase - spanSlope * weight);

            // Excel 特殊行为：WZL15.025.02 在有架空需求时永远 NG
            bool isWzl15 = m.Model == "WZL15.025.02";
            bool okSpan;
            if (request.MotionType == "升降")
                okSpan = false;
            else if (isWzl15 && needSpan > 0)
                okSpan = false;
            else
                okSpan = calcSpan >= needSpan;

            bool okFinal = okPrelim && okSpan;
            if (okFinal && finalModel == null) finalModel = m;

            matchResults.Add(new MatchResultDto
            {
                Model = m.Model,
                InnerHeight = m.InnerHeight,
                RecRadius = m.RecRadius,
                InnerArea = m.InnerArea ?? 0,
                CalcSpan = spanBase > 0 ? Math.Round(calcSpan) : 0,
                OkHeight = okH,
                OkRadius = okR,
                OkArea = okA,
                OkPrelim = okPrelim,
                OkSpan = okSpan,
                OkFinal = okFinal
            });
        }

        // 计算步骤
        var steps = new CalculationStepsDto
        {
            Step3_1_MinHeight = $"{minHeight:F2} mm",
            Step3_2_BendTube = tubeBend > 0 ? $"{tubeBend:F0} mm" : "—",
            Step3_2_BendCable = Math.Max(cableBend, encoderBend) > 0 ? $"{Math.Max(cableBend, encoderBend):F0} mm" : "—",
            Step3_2_BendMax = $"{maxBend:F0} mm",
            Step3_3_AreaSum = $"{totalArea:F2}",
            Step3_3_Ratio = $"{ratio * 100:F0} %",
            Step3_3_MinArea = $"{minArea:F2}",
        };

        decimal prelimLk = 0;
        if (prelimModel != null)
        {
            prelimLk = (decimal)Math.Ceiling((double)(request.Stroke / 2.0m + request.LmOffset + prelimModel.BendLength) / 10) * 10;
            steps.Step3_4_PrelimModel = prelimModel.Model;
            steps.Step3_5_PrelimLp = $"{prelimModel.BendLength:F0} mm";
            steps.Step3_5_PrelimLk = $"{prelimLk:F0} mm";
            steps.Step3_5_PrelimFull = $"{prelimModel.Model}-L{prelimLk:F0}";
        }
        else
        {
            steps.Step3_4_PrelimModel = "無";
        }

        steps.Step3_5_Motion = request.MotionType;
        steps.Step3_5_Stroke = $"{request.Stroke} mm";
        steps.Step3_5_Lm = $"{request.LmOffset} mm";

        decimal prelimSpan = 0;
        bool prelimSpanOk = false;
        if (prelimModel != null)
        {
            var spanData = wzlSpanData.GetValueOrDefault(prelimModel.Model);
            decimal maxW = spanData.maxW;
            decimal spanBase = spanData.spanBase;
            decimal spanSlope = spanData.spanSlope;
            prelimSpan = (weight > maxW) ? 0 : Math.Max(0, spanBase - spanSlope * weight);

            bool isWzl15 = prelimModel.Model == "WZL15.025.02";
            if (request.MotionType == "升降")
                prelimSpanOk = false;
            else if (isWzl15 && needSpan > 0)
                prelimSpanOk = false;
            else
                prelimSpanOk = prelimSpan >= needSpan;
        }

        steps.Step3_6_NeedSpan = needSpan > 0 ? $"{needSpan:F0} mm" : "升降無需計算";
        steps.Step3_6_Load = $"{weight:F4} kg/m";
        steps.Step3_6_SpanOk = prelimModel != null ? (prelimSpanOk ? "OK" : "NG") : "—";

        decimal finalLk = 0;
        if (finalModel != null)
        {
            finalLk = (decimal)Math.Ceiling((double)(request.Stroke / 2.0m + request.LmOffset + finalModel.BendLength) / 10) * 10;
            steps.Step3_6_FinalModel = finalModel.Model;
            steps.Step3_6_FinalLp = $"{finalModel.BendLength:F0} mm";
            steps.Step3_6_FinalLk = $"{finalLk:F0} mm";
        }
        else
        {
            steps.Step3_6_FinalModel = request.MotionType == "升降" ? "#N/A" : "無";
            steps.Step3_6_FinalLp = request.MotionType == "升降" ? "#N/A" : "—";
            steps.Step3_6_FinalLk = request.MotionType == "升降" ? "#N/A" : "—";
        }

        var response = new CalculationResponse
        {
            MinHeight = minHeight,
            MinRadius = maxBend,
            TotalArea = totalArea,
            MinInnerArea = minArea,
            TotalWeight = weight,
            NeedSpan = needSpan,
            CoreCount = cores,
            TubeBend = tubeBend,
            CableBend = cableBend,
            EncoderBend = encoderBend,
            MatchResults = matchResults,
            Steps = steps
        };

        if (finalModel != null)
        {
            response.PreliminaryModel = new SelectedModelDto
            {
                Model = prelimModel!.Model,
                Lp = prelimModel.BendLength,
                Lk = prelimLk,
                RecRadius = prelimModel.RecRadius,
                InnerArea = prelimModel.InnerArea ?? 0
            };
            response.FinalModel = new SelectedModelDto
            {
                Model = finalModel.Model,
                Lp = finalModel.BendLength,
                Lk = finalLk,
                RecRadius = finalModel.RecRadius,
                InnerArea = finalModel.InnerArea ?? 0
            };
            response.ResultStatus = "ok";
            response.ResultMessage = $"{finalModel.Model}-L{finalLk:F0}";
        }
        else if (request.MotionType == "升降" && prelimModel != null)
        {
            response.PreliminaryModel = new SelectedModelDto
            {
                Model = prelimModel.Model,
                Lp = prelimModel.BendLength,
                Lk = prelimLk,
                RecRadius = prelimModel.RecRadius,
                InnerArea = prelimModel.InnerArea ?? 0
            };
            response.ResultStatus = "warn";
            response.ResultMessage = $"{prelimModel.Model}-L{prelimLk:F0}";
            response.StrategyNote = "升降模式下管線做垂直運動，無需架空判定，實際使用初步選定型號即可。";
        }
        else
        {
            response.ResultStatus = "err";
            response.ResultMessage = "無合適型號";
            response.StrategyNote = "對策4-1：可增加龍骨系統或加大拖鏈規格；對策4-2：可增加輔助輪支撐。";
        }

        return response;
    }

    private async Task<CalculationResponse> CalcMeAsync(
        CalculationRequest request, decimal minHeight, decimal maxBend, decimal minArea,
        decimal totalArea, decimal weight, decimal needSpan, int cores,
        decimal tubeBend, decimal cableBend, decimal encoderBend, decimal ratio)
    {
        var meModels = await _context.MeCatalog.ToListAsync();

        var matchResults = new List<MatchResultDto>();
        var prelimEntry = (Model: (MeCatalog?)null, Lp: 0m, Suffix: "");
        var finalEntry = (Model: (MeCatalog?)null, Lp: 0m, Suffix: "");

        foreach (var m in meModels)
        {
            var variants = new[]
            {
                (R: m.R1, Lp: m.Lp1, Suffix: m.R1Suffix),
                (R: m.R2, Lp: m.Lp2, Suffix: m.R2Suffix),
                (R: m.R3, Lp: m.Lp3, Suffix: m.R3Suffix),
            };

            foreach (var v in variants)
            {
                bool okH = minHeight <= m.InnerHeight;
                bool okR = maxBend <= v.R;
                bool okA = minArea <= m.InnerArea;
                bool okPrelim = okH && okR && okA;

                if (okPrelim && prelimEntry.Model == null)
                    prelimEntry = (m, v.Lp, v.Suffix);

                // 架空能力
                decimal calcSpan = (weight > m.MaxWeight) ? 0 : Math.Max(0, m.SpanBase - m.SpanSlope * weight);
                bool okSpan = request.MotionType == "升降" ? false : calcSpan > needSpan;
                bool okFinal = okPrelim && okSpan;

                if (okFinal && finalEntry.Model == null)
                    finalEntry = (m, v.Lp, v.Suffix);

                string displayModel = $"{m.BaseModel}{v.Suffix}";
                matchResults.Add(new MatchResultDto
                {
                    Model = displayModel,
                    InnerHeight = m.InnerHeight,
                    RecRadius = v.R,
                    InnerArea = m.InnerArea,
                    CalcSpan = Math.Round(calcSpan),
                    OkHeight = okH,
                    OkRadius = okR,
                    OkArea = okA,
                    OkPrelim = okPrelim,
                    OkSpan = okSpan,
                    OkFinal = okFinal
                });
            }
        }

        // 计算步骤
        var steps = new CalculationStepsDto
        {
            Step3_1_MinHeight = $"{minHeight:F2} mm",
            Step3_2_BendTube = tubeBend > 0 ? $"{tubeBend:F0} mm" : "—",
            Step3_2_BendCable = Math.Max(cableBend, encoderBend) > 0 ? $"{Math.Max(cableBend, encoderBend):F0} mm" : "—",
            Step3_2_BendMax = $"{maxBend:F0} mm",
            Step3_3_AreaSum = $"{totalArea:F2}",
            Step3_3_Ratio = $"{ratio * 100:F0} %",
            Step3_3_MinArea = $"{minArea:F2}",
        };

        decimal prelimLk = 0;
        string prelimFullModel = "";
        if (prelimEntry.Model != null)
        {
            prelimLk = (decimal)Math.Ceiling((double)(request.Stroke / 2.0m + request.LmOffset + prelimEntry.Lp) / 10) * 10;
            prelimFullModel = $"{prelimEntry.Model.BaseModel}{prelimEntry.Suffix}";
            steps.Step3_4_PrelimModel = prelimFullModel;
            steps.Step3_5_PrelimLp = $"{prelimEntry.Lp:F0} mm";
            steps.Step3_5_PrelimLk = $"{prelimLk:F0} mm";
            steps.Step3_5_PrelimFull = $"{prelimFullModel}-L{prelimLk:F0}";
        }
        else
        {
            steps.Step3_4_PrelimModel = "無";
        }

        steps.Step3_5_Motion = request.MotionType;
        steps.Step3_5_Stroke = $"{request.Stroke} mm";
        steps.Step3_5_Lm = $"{request.LmOffset} mm";

        decimal prelimSpan = 0;
        bool prelimSpanOk = false;
        if (prelimEntry.Model != null)
        {
            prelimSpan = (weight > prelimEntry.Model.MaxWeight) ? 0
                        : Math.Max(0, prelimEntry.Model.SpanBase - prelimEntry.Model.SpanSlope * weight);
            prelimSpanOk = request.MotionType == "升降" ? false : prelimSpan > needSpan;
        }

        steps.Step3_6_NeedSpan = needSpan > 0 ? $"{needSpan:F0} mm" : "升降無需計算";
        steps.Step3_6_Load = $"{weight:F4} kg/m";
        steps.Step3_6_SpanOk = prelimEntry.Model != null ? (prelimSpanOk ? "OK" : "NG") : "—";

        decimal finalLk = 0;
        if (finalEntry.Model != null)
        {
            finalLk = (decimal)Math.Ceiling((double)(request.Stroke / 2.0m + request.LmOffset + finalEntry.Lp) / 10) * 10;
            string finalFullModel = $"{finalEntry.Model.BaseModel}{finalEntry.Suffix}";
            steps.Step3_6_FinalModel = finalFullModel;
            steps.Step3_6_FinalLp = $"{finalEntry.Lp:F0} mm";
            steps.Step3_6_FinalLk = $"{finalLk:F0} mm";
        }
        else
        {
            steps.Step3_6_FinalModel = request.MotionType == "升降" ? "#N/A" : "無";
            steps.Step3_6_FinalLp = request.MotionType == "升降" ? "#N/A" : "—";
            steps.Step3_6_FinalLk = request.MotionType == "升降" ? "#N/A" : "—";
        }

        var response = new CalculationResponse
        {
            MinHeight = minHeight,
            MinRadius = maxBend,
            TotalArea = totalArea,
            MinInnerArea = minArea,
            TotalWeight = weight,
            NeedSpan = needSpan,
            CoreCount = cores,
            TubeBend = tubeBend,
            CableBend = cableBend,
            EncoderBend = encoderBend,
            MatchResults = matchResults,
            Steps = steps
        };

        if (finalEntry.Model != null)
        {
            string finalFullModel = $"{finalEntry.Model.BaseModel}{finalEntry.Suffix}";
            if (prelimEntry.Model is { } preliminaryModel)
            {
                response.PreliminaryModel = new SelectedModelDto
                {
                    Model = prelimFullModel,
                    Lp = prelimEntry.Lp,
                    Lk = prelimLk,
                    RecRadius = preliminaryModel.InnerHeight, // ME 不存储单型号 RecRadius
                    InnerArea = preliminaryModel.InnerArea
                };
            }
            response.FinalModel = new SelectedModelDto
            {
                Model = finalFullModel,
                Lp = finalEntry.Lp,
                Lk = finalLk,
                RecRadius = finalEntry.Model.InnerHeight,
                InnerArea = finalEntry.Model.InnerArea
            };
            response.ResultStatus = "ok";
            response.ResultMessage = $"{finalFullModel}-L{finalLk:F0}";
        }
        else if (request.MotionType == "升降" && prelimEntry.Model != null)
        {
            response.PreliminaryModel = new SelectedModelDto
            {
                Model = prelimFullModel,
                Lp = prelimEntry.Lp,
                Lk = prelimLk,
                RecRadius = prelimEntry.Model.InnerHeight,
                InnerArea = prelimEntry.Model.InnerArea
            };
            response.ResultStatus = "warn";
            response.ResultMessage = $"{prelimFullModel}-L{prelimLk:F0}";
            response.StrategyNote = "升降模式下管線做垂直運動，無需架空判定，實際使用初步選定型號即可。";
        }
        else
        {
            response.ResultStatus = "err";
            response.ResultMessage = "無合適型號";
            response.StrategyNote = "對策4-1：可增加龍骨系統或加大拖鏈規格；對策4-2：可增加輔助輪支撐。";
        }

        return response;
    }

    private CalculationStepsDto BuildEmptySteps()
    {
        return new CalculationStepsDto
        {
            Step3_1_MinHeight = "–",
            Step3_2_BendTube = "–",
            Step3_2_BendCable = "–",
            Step3_2_BendMax = "–",
            Step3_3_AreaSum = "–",
            Step3_3_Ratio = "–",
            Step3_3_MinArea = "–",
            Step3_4_PrelimModel = "–",
            Step3_5_PrelimLp = "–",
            Step3_5_PrelimLk = "–",
            Step3_5_PrelimFull = "–",
            Step3_6_SpanOk = "–",
            Step3_6_FinalModel = "–",
            Step3_6_FinalLp = "–",
            Step3_6_FinalLk = "–",
        };
    }
}
