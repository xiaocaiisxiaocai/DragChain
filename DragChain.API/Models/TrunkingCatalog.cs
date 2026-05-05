using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("TrunkingCatalog")]
public class TrunkingCatalog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Model { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,2)")]
    public decimal Width { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Height { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerWidth { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerHeight { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal CrossSection { get; set; }

    [MaxLength(50)]
    public string Material { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Remarks { get; set; } = string.Empty;
}
