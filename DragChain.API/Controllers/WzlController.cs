using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WzlController : ControllerBase
{
    private readonly DragChainDbContext _context;

    public WzlController(DragChainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WzlCatalog>>> GetAll()
    {
        return await _context.WzlCatalog.OrderBy(w => w.Id).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<WzlCatalog>> Create([FromBody] CreateWzlCatalogDto dto)
    {
        var maxId = await _context.WzlCatalog.MaxAsync(w => (int?)w.Id) ?? 0;
        var wzl = new WzlCatalog
        {
            Id = maxId + 1,
            Model = dto.Model,
            Function = dto.Function,
            Stroke = dto.Stroke,
            InnerHeight = dto.InnerHeight,
            InnerWidth = dto.InnerWidth,
            OuterHeight = dto.OuterHeight,
            OuterWidth = dto.OuterWidth,
            MinRadius = dto.MinRadius,
            RecRadius = dto.RecRadius,
            ReservedK = dto.ReservedK,
            BendLength = dto.BendLength,
            MountingH1 = dto.MountingH1,
            InterferenceH2 = dto.InterferenceH2,
            InnerArea = dto.InnerArea,
            AppPipes = dto.AppPipes
        };
        _context.WzlCatalog.Add(wzl);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), wzl);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWzlCatalogDto dto)
    {
        var wzl = await _context.WzlCatalog.FindAsync(id);
        if (wzl == null) return NotFound();

        if (dto.Model != null) wzl.Model = dto.Model;
        if (dto.Function != null) wzl.Function = dto.Function;
        if (dto.Stroke != null) wzl.Stroke = dto.Stroke;
        if (dto.InnerHeight.HasValue) wzl.InnerHeight = dto.InnerHeight.Value;
        if (dto.InnerWidth.HasValue) wzl.InnerWidth = dto.InnerWidth.Value;
        if (dto.OuterHeight.HasValue) wzl.OuterHeight = dto.OuterHeight.Value;
        if (dto.OuterWidth.HasValue) wzl.OuterWidth = dto.OuterWidth.Value;
        if (dto.MinRadius.HasValue) wzl.MinRadius = dto.MinRadius.Value;
        if (dto.RecRadius.HasValue) wzl.RecRadius = dto.RecRadius.Value;
        if (dto.ReservedK.HasValue) wzl.ReservedK = dto.ReservedK.Value;
        if (dto.BendLength.HasValue) wzl.BendLength = dto.BendLength.Value;
        if (dto.MountingH1 != null) wzl.MountingH1 = dto.MountingH1;
        if (dto.InterferenceH2 != null) wzl.InterferenceH2 = dto.InterferenceH2;
        if (dto.InnerArea.HasValue) wzl.InnerArea = dto.InnerArea.Value;
        if (dto.AppPipes != null) wzl.AppPipes = dto.AppPipes;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var wzl = await _context.WzlCatalog.FindAsync(id);
        if (wzl == null) return NotFound();
        _context.WzlCatalog.Remove(wzl);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await CatalogSeeder.ResetAsync(_context);
        return Ok(new { message = "WZL 型錄已恢復預設" });
    }
}
