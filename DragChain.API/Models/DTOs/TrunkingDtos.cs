using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class TrunkingCalcRequest
{
    public int SelectedTrunkingId { get; set; }
    public decimal FillRatio { get; set; } = 0.75m;
    public List<PipeItemDto> Pipes { get; set; } = new();
}

public class TrunkingCalcResponse
{
    public decimal TotalArea { get; set; }
    public decimal FillRatio { get; set; }
    public decimal ActualFillRatio { get; set; }
    public decimal MaxPipeDia { get; set; }
    public int TotalPipeCount { get; set; }

    public TrunkingCatalogDto? SelectedTrunking { get; set; }
    public TrunkingStepsDto Steps { get; set; } = new();
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}

public class TrunkingCatalogDto
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal InnerWidth { get; set; }
    public decimal InnerHeight { get; set; }
    public decimal CrossSection { get; set; }
    public string Material { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
}

public class TrunkingStepsDto
{
    public string Step1_TotalArea { get; set; } = string.Empty;
    public string Step1_MaxDia { get; set; } = string.Empty;
    public string Step1_PipeCount { get; set; } = string.Empty;
    public string Step2_TrunkingArea { get; set; } = string.Empty;
    public string Step2_FillRatio { get; set; } = string.Empty;
    public string Step3_Result { get; set; } = string.Empty;
}

public class CreateTrunkingCatalogDto
{
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal InnerWidth { get; set; }
    public decimal InnerHeight { get; set; }
    public decimal CrossSection { get; set; }

    [MaxLength(50)]
    public string Material { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Remarks { get; set; } = string.Empty;
}

public class UpdateTrunkingCatalogDto
{
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal InnerWidth { get; set; }
    public decimal InnerHeight { get; set; }
    public decimal CrossSection { get; set; }

    [MaxLength(50)]
    public string Material { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Remarks { get; set; } = string.Empty;
}
