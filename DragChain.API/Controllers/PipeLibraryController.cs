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
        return await _context.PipeTypes.OrderBy(p => p.Id).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<PipeType>> Create([FromBody] CreatePipeTypeDto dto)
    {
        var maxId = await _context.PipeTypes.MaxAsync(p => (int?)p.Id) ?? 0;
        var pipe = new PipeType
        {
            Id = maxId + 1,
            Name = dto.Name,
            Type = dto.Type,
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
        if (dto.Type != null) pipe.Type = dto.Type;
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
