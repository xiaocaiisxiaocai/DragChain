using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class CreateMeCatalogDto
{
    [Required]
    [MaxLength(50)]
    public string BaseModel { get; set; } = string.Empty;

    public decimal InnerHeight { get; set; }
    public decimal InnerWidth { get; set; }
    public decimal R1 { get; set; }
    public decimal R2 { get; set; }
    public decimal R3 { get; set; }

    [MaxLength(10)]
    public string R1Suffix { get; set; } = string.Empty;

    [MaxLength(10)]
    public string R2Suffix { get; set; } = string.Empty;

    [MaxLength(10)]
    public string R3Suffix { get; set; } = string.Empty;

    public decimal Lp1 { get; set; }
    public decimal Lp2 { get; set; }
    public decimal Lp3 { get; set; }
    public decimal InnerArea { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal SpanBase { get; set; }
    public decimal SpanSlope { get; set; }
}

public class UpdateMeCatalogDto
{
    [MaxLength(50)]
    public string? BaseModel { get; set; }

    public decimal? InnerHeight { get; set; }
    public decimal? InnerWidth { get; set; }
    public decimal? R1 { get; set; }
    public decimal? R2 { get; set; }
    public decimal? R3 { get; set; }

    [MaxLength(10)]
    public string? R1Suffix { get; set; }

    [MaxLength(10)]
    public string? R2Suffix { get; set; }

    [MaxLength(10)]
    public string? R3Suffix { get; set; }

    public decimal? Lp1 { get; set; }
    public decimal? Lp2 { get; set; }
    public decimal? Lp3 { get; set; }
    public decimal? InnerArea { get; set; }
    public decimal? MaxWeight { get; set; }
    public decimal? SpanBase { get; set; }
    public decimal? SpanSlope { get; set; }
}
