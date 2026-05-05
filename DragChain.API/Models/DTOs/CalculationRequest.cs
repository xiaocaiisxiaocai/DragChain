using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class CalculationRequest
{
    [Required]
    public string Brand { get; set; } = "wzl"; // "wzl" | "me"

    public int SensorCount { get; set; } = 0;

    public int MagnetCount { get; set; } = 0;

    [Required]
    public string MotionType { get; set; } = "横移"; // "横移" | "升降"

    public int Stroke { get; set; } = 0;

    public int LmOffset { get; set; } = 0;

    public List<PipeItemDto> Pipes { get; set; } = new();
}

public class PipeItemDto
{
    public int PipeTypeId { get; set; }
    public int Qty { get; set; }
}
