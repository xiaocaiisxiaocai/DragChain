using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipeComponentsController : ControllerBase
{
    private readonly DragChainDbContext _context;

    public PipeComponentsController(DragChainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PipeComponentDto>>> GetAll()
    {
        var components = await QueryComponents().ToListAsync();
        return components
            .OrderBy(component => component.Name)
            .ThenBy(component => component.Id)
            .Select(MapToDto)
            .ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PipeComponentDto>> GetById(int id)
    {
        var component = await QueryComponents().FirstOrDefaultAsync(component => component.Id == id);
        if (component == null) return NotFound();
        return MapToDto(component);
    }

    [HttpPost]
    public async Task<ActionResult<PipeComponentDto>> Create([FromBody] CreatePipeComponentDto dto)
    {
        var validation = await ValidateItemsAsync(dto.Items);
        if (validation != null) return validation;

        var maxId = await _context.PipeComponents.MaxAsync(component => (int?)component.Id) ?? 0;
        var component = new PipeComponent
        {
            Id = maxId + 1,
            Name = dto.Name.Trim(),
            Description = dto.Description.Trim(),
            Items = BuildItems(dto.Items)
        };

        _context.PipeComponents.Add(component);
        await _context.SaveChangesAsync();

        var saved = await QueryComponents().FirstAsync(item => item.Id == component.Id);
        return CreatedAtAction(nameof(GetById), new { id = saved.Id }, MapToDto(saved));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePipeComponentDto dto)
    {
        var component = await _context.PipeComponents
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (component == null) return NotFound();

        var validation = await ValidateItemsAsync(dto.Items);
        if (validation != null) return validation;

        component.Name = dto.Name.Trim();
        component.Description = dto.Description.Trim();
        component.Items.Clear();
        foreach (var item in BuildItems(dto.Items))
        {
            component.Items.Add(item);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var component = await _context.PipeComponents.FindAsync(id);
        if (component == null) return NotFound();

        _context.PipeComponents.Remove(component);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<PipeComponent> QueryComponents() =>
        _context.PipeComponents
            .Include(component => component.Items.OrderBy(item => item.Id))
            .ThenInclude(item => item.PipeType);

    private async Task<ActionResult?> ValidateItemsAsync(List<CreatePipeComponentItemDto> items)
    {
        var validItems = items.Where(item => item.PipeTypeId > 0 && item.Qty > 0).ToList();
        if (validItems.Count == 0) return BadRequest("元件至少需要包含一条管线。");

        var pipeIds = validItems.Select(item => item.PipeTypeId).Distinct().ToList();
        var existingCount = await _context.PipeTypes.CountAsync(pipe => pipeIds.Contains(pipe.Id));
        if (existingCount != pipeIds.Count) return BadRequest("元件包含不存在的管线。");

        return null;
    }

    private static List<PipeComponentItem> BuildItems(List<CreatePipeComponentItemDto> items) =>
        items
            .Where(item => item.PipeTypeId > 0 && item.Qty > 0)
            .GroupBy(item => new { item.PipeTypeId, Layer = NormalizeLayer(item.Layer) })
            .Select(group => new PipeComponentItem
            {
                PipeTypeId = group.Key.PipeTypeId,
                Qty = group.Sum(item => item.Qty),
                Layer = group.Key.Layer
            })
            .ToList();

    private static string NormalizeLayer(string? layer) => layer == "bottom" ? "bottom" : "top";

    private static PipeComponentDto MapToDto(PipeComponent component) => new()
    {
        Id = component.Id,
        Name = component.Name,
        Description = component.Description,
        Items = component.Items
            .OrderBy(item => item.PipeType?.Type)
            .ThenBy(item => item.PipeType?.Diameter)
            .ThenBy(item => item.PipeType?.Name)
            .Select(item => new PipeComponentItemDto
            {
                Id = item.Id,
                ComponentId = item.PipeComponentId,
                PipeTypeId = item.PipeTypeId,
                Qty = item.Qty,
                Layer = NormalizeLayer(item.Layer),
                PipeType = item.PipeType
            })
            .ToList()
    };
}
