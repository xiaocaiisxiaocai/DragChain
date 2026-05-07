using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class CreatePipeTypeDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "weak_cable";

    public decimal Diameter { get; set; }

    public decimal Weight { get; set; }

    public int BendMultiplier { get; set; } = 8;
}

public class UpdatePipeTypeDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(20)]
    public string? Type { get; set; }

    public decimal? Diameter { get; set; }

    public decimal? Weight { get; set; }

    public int? BendMultiplier { get; set; }
}
