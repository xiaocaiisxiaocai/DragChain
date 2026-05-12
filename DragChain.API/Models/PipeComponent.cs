using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Models;

[Table("PipeComponents")]
public class PipeComponent
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<PipeComponentItem> Items { get; set; } = new();
}

[Table("PipeComponentItems")]
public class PipeComponentItem
{
    [Key]
    public int Id { get; set; }

    public int PipeComponentId { get; set; }

    public int PipeTypeId { get; set; }

    public int Qty { get; set; } = 1;

    public PipeComponent? PipeComponent { get; set; }

    public PipeType? PipeType { get; set; }
}
