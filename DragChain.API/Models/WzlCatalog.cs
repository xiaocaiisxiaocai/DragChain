using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("WzlCatalog")]
public class WzlCatalog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Function { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Stroke { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerHeight { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerWidth { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal OuterHeight { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal OuterWidth { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal MinRadius { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal RecRadius { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal ReservedK { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal BendLength { get; set; }

    [MaxLength(50)]
    public string MountingH1 { get; set; } = string.Empty;

    [MaxLength(50)]
    public string InterferenceH2 { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? InnerArea { get; set; }

    [MaxLength(200)]
    public string AppPipes { get; set; } = string.Empty;
}
