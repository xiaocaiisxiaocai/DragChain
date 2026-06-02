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

    [Column(TypeName = "decimal(10,2)")]
    public decimal CrossSection { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal FillRatioLimit { get; set; } = 0.60m;
}
