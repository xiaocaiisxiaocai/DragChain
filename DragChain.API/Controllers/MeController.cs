using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MeController : ControllerBase
{
    private readonly DragChainDbContext _context;

    public MeController(DragChainDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MeCatalog>>> GetAll()
    {
        return await _context.MeCatalog.OrderBy(m => m.Id).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<MeCatalog>> Create([FromBody] CreateMeCatalogDto dto)
    {
        var maxId = await _context.MeCatalog.MaxAsync(m => (int?)m.Id) ?? 0;
        var me = new MeCatalog
        {
            Id = maxId + 1,
            BaseModel = dto.BaseModel,
            FunctionSelect = dto.FunctionSelect,
            InnerHeight = dto.InnerHeight,
            InnerWidth = dto.InnerWidth,
            R1 = dto.R1, R2 = dto.R2, R3 = dto.R3,
            R1Suffix = dto.R1Suffix, R2Suffix = dto.R2Suffix, R3Suffix = dto.R3Suffix,
            Lp1 = dto.Lp1, Lp2 = dto.Lp2, Lp3 = dto.Lp3,
            MountingH1 = dto.MountingH1,
            InnerArea = dto.InnerArea,
            MaxWeight = dto.MaxWeight,
            SpanBase = dto.SpanBase,
            SpanSlope = dto.SpanSlope
        };
        _context.MeCatalog.Add(me);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), me);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMeCatalogDto dto)
    {
        var me = await _context.MeCatalog.FindAsync(id);
        if (me == null) return NotFound();

        if (dto.BaseModel != null) me.BaseModel = dto.BaseModel;
        if (dto.FunctionSelect != null) me.FunctionSelect = dto.FunctionSelect;
        if (dto.InnerHeight.HasValue) me.InnerHeight = dto.InnerHeight.Value;
        if (dto.InnerWidth.HasValue) me.InnerWidth = dto.InnerWidth.Value;
        if (dto.R1.HasValue) me.R1 = dto.R1.Value;
        if (dto.R2.HasValue) me.R2 = dto.R2.Value;
        if (dto.R3.HasValue) me.R3 = dto.R3.Value;
        if (dto.R1Suffix != null) me.R1Suffix = dto.R1Suffix;
        if (dto.R2Suffix != null) me.R2Suffix = dto.R2Suffix;
        if (dto.R3Suffix != null) me.R3Suffix = dto.R3Suffix;
        if (dto.Lp1.HasValue) me.Lp1 = dto.Lp1.Value;
        if (dto.Lp2.HasValue) me.Lp2 = dto.Lp2.Value;
        if (dto.Lp3.HasValue) me.Lp3 = dto.Lp3.Value;
        if (dto.MountingH1 != null) me.MountingH1 = dto.MountingH1;
        if (dto.InnerArea.HasValue) me.InnerArea = dto.InnerArea.Value;
        if (dto.MaxWeight.HasValue) me.MaxWeight = dto.MaxWeight.Value;
        if (dto.SpanBase.HasValue) me.SpanBase = dto.SpanBase.Value;
        if (dto.SpanSlope.HasValue) me.SpanSlope = dto.SpanSlope.Value;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var me = await _context.MeCatalog.FindAsync(id);
        if (me == null) return NotFound();
        _context.MeCatalog.Remove(me);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await CatalogSeeder.ResetAsync(_context);
        return Ok(new { message = "ME 型錄已恢復預設" });
    }
}
