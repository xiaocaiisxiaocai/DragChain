using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class CreateWzlCatalogDto
{
    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Function { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Stroke { get; set; } = string.Empty;

    public decimal InnerHeight { get; set; }
    public decimal InnerWidth { get; set; }
    public decimal OuterHeight { get; set; }
    public decimal OuterWidth { get; set; }
    public decimal MinRadius { get; set; }
    public decimal RecRadius { get; set; }
    public decimal ReservedK { get; set; }
    public decimal BendLength { get; set; }

    [MaxLength(50)]
    public string MountingH1 { get; set; } = string.Empty;

    [MaxLength(50)]
    public string InterferenceH2 { get; set; } = string.Empty;

    public decimal? InnerArea { get; set; }

    [MaxLength(200)]
    public string AppPipes { get; set; } = string.Empty;
}

public class UpdateWzlCatalogDto
{
    [MaxLength(50)]
    public string? Model { get; set; }

    [MaxLength(30)]
    public string? Function { get; set; }

    [MaxLength(20)]
    public string? Stroke { get; set; }

    public decimal? InnerHeight { get; set; }
    public decimal? InnerWidth { get; set; }
    public decimal? OuterHeight { get; set; }
    public decimal? OuterWidth { get; set; }
    public decimal? MinRadius { get; set; }
    public decimal? RecRadius { get; set; }
    public decimal? ReservedK { get; set; }
    public decimal? BendLength { get; set; }

    [MaxLength(50)]
    public string? MountingH1 { get; set; }

    [MaxLength(50)]
    public string? InterferenceH2 { get; set; }

    public decimal? InnerArea { get; set; }

    [MaxLength(200)]
    public string? AppPipes { get; set; }
}
