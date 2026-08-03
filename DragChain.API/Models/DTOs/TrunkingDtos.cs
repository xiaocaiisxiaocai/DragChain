using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DragChain.API.Models.DTOs;

public class TrunkingCalcRequest
{
    public int SelectedTrunkingId { get; set; }
    public decimal FillRatio { get; set; } = 0.60m;
    public string SlotOrder { get; set; } = "topToBottom";
    public List<PipeItemDto> Pipes { get; set; } = new();
    public List<TrunkingSlotRequestDto> Slots { get; set; } = new();
}

public class TrunkingCalcResponse
{
    public decimal TotalArea { get; set; }
    public decimal FillRatio { get; set; }
    public decimal ActualFillRatio { get; set; }
    public decimal MaxPipeDia { get; set; }
    public int TotalPipeCount { get; set; }

    public TrunkingCatalogDto? SelectedTrunking { get; set; }
    public List<TrunkingMatchResultDto> MatchResults { get; set; } = new();
    public TrunkingSideResultDto? WeakSide { get; set; }
    public TrunkingSideResultDto? StrongSide { get; set; }
    public List<TrunkingSlotResultDto> Slots { get; set; } = new();
    public List<TrunkingSlotResultDto> SideSlots { get; set; } = new();
    public TrunkingStepsDto Steps { get; set; } = new();
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}

public class TrunkingSlotRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Layout { get; set; } = "leftRight";
    public int? LeftTrunkingId { get; set; }
    public int? RightTrunkingId { get; set; }
    public decimal? LeftFillRatio { get; set; }
    public decimal? RightFillRatio { get; set; }
    public List<PipeItemDto> Pipes { get; set; } = new();
    public List<TrunkingSlotSectionRequestDto> Sections { get; set; } = new();
}

public class TrunkingSlotSectionRequestDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int? SelectedTrunkingId { get; set; }
    public decimal? FillRatio { get; set; }
    public List<PipeItemDto> Pipes { get; set; } = new();
}

public class TrunkingSlotResultDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Layout { get; set; } = string.Empty;
    public List<TrunkingSideResultDto> Sections { get; set; } = new();
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}

public class TrunkingSideResultDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal TotalArea { get; set; }
    public decimal FillRatio { get; set; }
    public decimal ActualFillRatio { get; set; }
    public decimal MaxPipeDia { get; set; }
    public int TotalPipeCount { get; set; }
    public TrunkingCatalogDto? SelectedTrunking { get; set; }
    public List<TrunkingMatchResultDto> MatchResults { get; set; } = new();
    public string ResultStatus { get; set; } = string.Empty;
    public string ResultMessage { get; set; } = string.Empty;
}

public class TrunkingMatchResultDto
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal CrossSection { get; set; }
    public decimal FillRatioLimit { get; set; }
    public decimal ActualFillRatio { get; set; }
    public bool OkFill { get; set; }
    public bool IsRecommended { get; set; }
    public string Result { get; set; } = string.Empty;
}

public class TrunkingCatalogDto
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal CrossSection { get; set; }
    public decimal FillRatioLimit { get; set; }
}

public class TrunkingSettingsDto
{
    public decimal FillRatio { get; set; } = 0.60m;
}

public class TrunkingSavedSelectionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime SavedAt { get; set; }
    public TrunkingCalcRequest Request { get; set; } = new();
    public TrunkingCalcResponse? Result { get; set; }
    public JsonElement? SourceSlots { get; set; }
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
    public decimal CrossSection { get; set; }
    public decimal FillRatioLimit { get; set; } = 0.60m;
}

public class UpdateTrunkingCatalogDto
{
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal CrossSection { get; set; }
    public decimal FillRatioLimit { get; set; } = 0.60m;
}
