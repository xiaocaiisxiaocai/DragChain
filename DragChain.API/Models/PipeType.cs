using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("PipeTypes")]
public class PipeType
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Type { get; set; } = "cable"; // tube / cable / encoder / other

    [Column(TypeName = "decimal(8,2)")]
    public decimal Diameter { get; set; }

    [Column(TypeName = "decimal(10,4)")]
    public decimal Weight { get; set; }

    public int BendMultiplier { get; set; } = 8; // 8 = 普通, 13 = 编码器线
}
