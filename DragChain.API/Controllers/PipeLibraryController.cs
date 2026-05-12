using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipeLibraryController : ControllerBase
{
    private readonly DragChainDbContext _context;

    public PipeLibraryController(DragChainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PipeType>>> GetAll()
    {
        var pipes = await _context.PipeTypes.ToListAsync();
        return pipes
            .OrderBy(GetTypeSort)
            .ThenBy(p => p.Diameter)
            .ThenBy(p => p.Weight)
            .ThenBy(p => p.BendMultiplier)
            .ThenBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToList();
    }

    private static int GetTypeSort(PipeType pipe) => pipe.Type switch
    {
        "weak_cable" => 10,
        "encoder" => 20,
        "strong_cable" => 30,
        "tube" => 40,
        _ => 90
    };

    [HttpPost]
    public async Task<ActionResult<PipeType>> Create([FromBody] CreatePipeTypeDto dto)
    {
        var maxId = await _context.PipeTypes.MaxAsync(p => (int?)p.Id) ?? 0;
        var pipe = new PipeType
        {
            Id = maxId + 1,
            Name = dto.Name,
            Type = PipeTypeCategory.Normalize(dto.Type),
            Diameter = dto.Diameter,
            Weight = dto.Weight,
            BendMultiplier = dto.BendMultiplier
        };
        _context.PipeTypes.Add(pipe);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), pipe);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePipeTypeDto dto)
    {
        var pipe = await _context.PipeTypes.FindAsync(id);
        if (pipe == null) return NotFound();

        if (dto.Name != null) pipe.Name = dto.Name;
        if (dto.Type != null) pipe.Type = PipeTypeCategory.Normalize(dto.Type);
        if (dto.Diameter.HasValue) pipe.Diameter = dto.Diameter.Value;
        if (dto.Weight.HasValue) pipe.Weight = dto.Weight.Value;
        if (dto.BendMultiplier.HasValue) pipe.BendMultiplier = dto.BendMultiplier.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var pipe = await _context.PipeTypes.FindAsync(id);
        if (pipe == null) return NotFound();

        var usedByModule = await _context.PipeModuleItems.AnyAsync(item => item.PipeTypeId == id);
        var usedByComponent = await _context.PipeComponentItems.AnyAsync(item => item.PipeTypeId == id);
        if (usedByModule || usedByComponent) return Conflict("该管线已被模块或元件引用，请先调整模块或元件内容。");

        _context.PipeTypes.Remove(pipe);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await CatalogSeeder.ResetAsync(_context);
        return Ok(new { message = "管線庫已恢復預設" });
    }
}
