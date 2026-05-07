using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("MeCatalog")]
public class MeCatalog
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string BaseModel { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FunctionSelect { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerHeight { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal InnerWidth { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal R1 { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal R2 { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal R3 { get; set; }

    [MaxLength(10)]
    public string R1Suffix { get; set; } = string.Empty;

    [MaxLength(10)]
    public string R2Suffix { get; set; } = string.Empty;

    [MaxLength(10)]
    public string R3Suffix { get; set; } = string.Empty;

    [Column(TypeName = "decimal(8,2)")]
    public decimal Lp1 { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Lp2 { get; set; }

    [Column(TypeName = "decimal(8,2)")]
    public decimal Lp3 { get; set; }

    [MaxLength(50)]
    public string MountingH1 { get; set; } = string.Empty;

    [Column(TypeName = "decimal(10,2)")]
    public decimal InnerArea { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxWeight { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal SpanBase { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal SpanSlope { get; set; }
}
