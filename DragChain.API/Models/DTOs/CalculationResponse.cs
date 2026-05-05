namespace DragChain.API.Models.DTOs;

public class CalculationResponse
{
    public decimal MinHeight { get; set; }
    public decimal MinRadius { get; set; }
    public decimal TotalArea { get; set; }
    public decimal MinInnerArea { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal NeedSpan { get; set; }
    public int CoreCount { get; set; }
    public decimal TubeBend { get; set; }
    public decimal CableBend { get; set; }
    public decimal EncoderBend { get; set; }

    public List<MatchResultDto> MatchResults { get; set; } = new();
    public SelectedModelDto? PreliminaryModel { get; set; }
    public SelectedModelDto? FinalModel { get; set; }
    public CalculationStepsDto Steps { get; set; } = new();
    public string ResultStatus { get; set; } = string.Empty; // "ok" | "warn" | "err"
    public string ResultMessage { get; set; } = string.Empty;
    public string? StrategyNote { get; set; }
}

public class MatchResultDto
{
    public string Model { get; set; } = string.Empty;
    public decimal InnerHeight { get; set; }
    public decimal RecRadius { get; set; }
    public decimal InnerArea { get; set; }
    public decimal CalcSpan { get; set; }
    public bool OkHeight { get; set; }
    public bool OkRadius { get; set; }
    public bool OkArea { get; set; }
    public bool OkPrelim { get; set; }
    public bool OkSpan { get; set; }
    public bool OkFinal { get; set; }
}

public class SelectedModelDto
{
    public string Model { get; set; } = string.Empty;
    public decimal Lp { get; set; }
    public decimal Lk { get; set; }
    public decimal RecRadius { get; set; }
    public decimal InnerArea { get; set; }
}

public class CalculationStepsDto
{
    public string Step3_1_MinHeight { get; set; } = string.Empty;
    public string Step3_2_BendTube { get; set; } = string.Empty;
    public string Step3_2_BendCable { get; set; } = string.Empty;
    public string Step3_2_BendMax { get; set; } = string.Empty;
    public string Step3_3_AreaSum { get; set; } = string.Empty;
    public string Step3_3_Ratio { get; set; } = string.Empty;
    public string Step3_3_MinArea { get; set; } = string.Empty;
    public string Step3_4_PrelimModel { get; set; } = string.Empty;
    public string Step3_5_Motion { get; set; } = string.Empty;
    public string Step3_5_Stroke { get; set; } = string.Empty;
    public string Step3_5_Lm { get; set; } = string.Empty;
    public string Step3_5_PrelimLp { get; set; } = string.Empty;
    public string Step3_5_PrelimLk { get; set; } = string.Empty;
    public string Step3_5_PrelimFull { get; set; } = string.Empty;
    public string Step3_6_NeedSpan { get; set; } = string.Empty;
    public string Step3_6_Load { get; set; } = string.Empty;
    public string Step3_6_SpanOk { get; set; } = string.Empty;
    public string Step3_6_FinalModel { get; set; } = string.Empty;
    public string Step3_6_FinalLp { get; set; } = string.Empty;
    public string Step3_6_FinalLk { get; set; } = string.Empty;
}
