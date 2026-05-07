using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipeModulesController : ControllerBase
{
    private readonly DragChainDbContext _context;

    public PipeModulesController(DragChainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PipeModuleDto>>> GetAll()
    {
        var modules = await QueryModules().ToListAsync();
        return modules
            .OrderBy(module => module.Name)
            .ThenBy(module => module.Id)
            .Select(MapToDto)
            .ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PipeModuleDto>> GetById(int id)
    {
        var module = await QueryModules().FirstOrDefaultAsync(module => module.Id == id);
        if (module == null) return NotFound();
        return MapToDto(module);
    }

    [HttpPost]
    public async Task<ActionResult<PipeModuleDto>> Create([FromBody] CreatePipeModuleDto dto)
    {
        var validation = await ValidateItemsAsync(dto.Items);
        if (validation != null) return validation;

        var maxId = await _context.PipeModules.MaxAsync(module => (int?)module.Id) ?? 0;
        var module = new PipeModule
        {
            Id = maxId + 1,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Items = BuildItems(dto.Items)
        };

        _context.PipeModules.Add(module);
        await _context.SaveChangesAsync();

        var saved = await QueryModules().FirstAsync(item => item.Id == module.Id);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, MapToDto(saved));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePipeModuleDto dto)
    {
        var module = await _context.PipeModules
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (module == null) return NotFound();

        var validation = await ValidateItemsAsync(dto.Items);
        if (validation != null) return validation;

        module.Name = dto.Name.Trim();
        module.Description = dto.Description.Trim();
        module.Items.Clear();
        foreach (var item in BuildItems(dto.Items))
        {
            module.Items.Add(item);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var module = await _context.PipeModules.FindAsync(id);
        if (module == null) return NotFound();

        _context.PipeModules.Remove(module);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<PipeModule> QueryModules() =>
        _context.PipeModules
            .Include(module => module.Items.OrderBy(item => item.Id))
            .ThenInclude(item => item.PipeType);

    private async Task<ActionResult?> ValidateItemsAsync(List<CreatePipeModuleItemDto> items)
    {
        var validItems = items.Where(item => item.PipeTypeId > 0 && item.Qty > 0).ToList();
        if (validItems.Count == 0) return BadRequest("模块至少需要包含一条管线。");

        var pipeIds = validItems.Select(item => item.PipeTypeId).Distinct().ToList();
        var existingCount = await _context.PipeTypes.CountAsync(pipe => pipeIds.Contains(pipe.Id));
        if (existingCount != pipeIds.Count) return BadRequest("模块包含不存在的管线。");

        return null;
    }

    private static List<PipeModuleItem> BuildItems(List<CreatePipeModuleItemDto> items) =>
        items
            .Where(item => item.PipeTypeId > 0 && item.Qty > 0)
            .GroupBy(item => item.PipeTypeId)
            .Select(group => new PipeModuleItem
            {
                PipeTypeId = group.Key,
                Qty = group.Sum(item => item.Qty)
            })
            .ToList();

    private static PipeModuleDto MapToDto(PipeModule module) => new()
    {
        Id = module.Id,
        Name = module.Name,
        Description = module.Description,
        Items = module.Items
            .OrderBy(item => item.PipeType?.Type)
            .ThenBy(item => item.PipeType?.Diameter)
            .ThenBy(item => item.PipeType?.Name)
            .Select(item => new PipeModuleItemDto
            {
                Id = item.Id,
                ModuleId = item.PipeModuleId,
                PipeTypeId = item.PipeTypeId,
                Qty = item.Qty,
                PipeType = item.PipeType
            })
            .ToList()
    };
}
