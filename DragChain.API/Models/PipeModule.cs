using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("PipeModules")]
public class PipeModule
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<PipeModuleItem> Items { get; set; } = new();
}

[Table("PipeModuleItems")]
public class PipeModuleItem
{
    [Key]
    public int Id { get; set; }

    public int PipeModuleId { get; set; }

    public int PipeTypeId { get; set; }

    public int Qty { get; set; } = 1;

    public PipeModule? PipeModule { get; set; }

    public PipeType? PipeType { get; set; }
}
