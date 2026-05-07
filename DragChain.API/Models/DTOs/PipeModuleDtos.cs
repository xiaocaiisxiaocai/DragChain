using System.ComponentModel.DataAnnotations;

namespace DragChain.API.Models.DTOs;

public class PipeModuleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<PipeModuleItemDto> Items { get; set; } = new();
}

public class PipeModuleItemDto
{
    public int Id { get; set; }
    public int ModuleId { get; set; }
    public int PipeTypeId { get; set; }
    public int Qty { get; set; }
    public PipeType? PipeType { get; set; }
}

public class CreatePipeModuleDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public List<CreatePipeModuleItemDto> Items { get; set; } = new();
}

public class UpdatePipeModuleDto : CreatePipeModuleDto
{
}

public class CreatePipeModuleItemDto
{
    public int PipeTypeId { get; set; }
    public int Qty { get; set; } = 1;
}
