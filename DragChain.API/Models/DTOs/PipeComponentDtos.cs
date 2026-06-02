using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class PipeComponentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PipeComponentItemDto> Items { get; set; } = new();
}

public class PipeComponentItemDto
{
    public int Id { get; set; }
    public int ComponentId { get; set; }
    public int PipeTypeId { get; set; }
    public int Qty { get; set; }
    public string Layer { get; set; } = "top";
    public PipeType? PipeType { get; set; }
}

public class CreatePipeComponentDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<CreatePipeComponentItemDto> Items { get; set; } = new();
}

public class UpdatePipeComponentDto : CreatePipeComponentDto
{
}

public class CreatePipeComponentItemDto
{
    public int PipeTypeId { get; set; }
    public int Qty { get; set; } = 1;
    public string Layer { get; set; } = "top";
}
