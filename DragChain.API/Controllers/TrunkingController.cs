using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;
using DragChain.API.Models.DTOs;
using DragChain.API.Services;

namespace DragChain.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TrunkingController : ControllerBase
{
    private readonly DragChainDbContext _context;
    private readonly ITrunkingCalculationService _calcService;

    public TrunkingController(DragChainDbContext context, ITrunkingCalculationService calcService)
    {
        _context = context;
        _calcService = calcService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrunkingCatalog>>> GetAll()
    {
        return await _context.TrunkingCatalog.OrderBy(t => t.Id).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<TrunkingCatalog>> Create([FromBody] CreateTrunkingCatalogDto dto)
    {
        var maxId = await _context.TrunkingCatalog.MaxAsync(t => (int?)t.Id) ?? 0;
        var tk = new TrunkingCatalog
        {
            Id = maxId + 1,
            Model = dto.Model,
            Width = dto.Width,
            Height = dto.Height,
            InnerWidth = dto.InnerWidth,
            InnerHeight = dto.InnerHeight,
            CrossSection = dto.CrossSection,
            Material = dto.Material,
            Remarks = dto.Remarks
        };
        _context.TrunkingCatalog.Add(tk);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), tk);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrunkingCatalogDto dto)
    {
        var tk = await _context.TrunkingCatalog.FindAsync(id);
        if (tk == null) return NotFound();

        tk.Model = dto.Model;
        tk.Width = dto.Width;
        tk.Height = dto.Height;
        tk.InnerWidth = dto.InnerWidth;
        tk.InnerHeight = dto.InnerHeight;
        tk.CrossSection = dto.CrossSection;
        tk.Material = dto.Material;
        tk.Remarks = dto.Remarks;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var tk = await _context.TrunkingCatalog.FindAsync(id);
        if (tk == null) return NotFound();
        _context.TrunkingCatalog.Remove(tk);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("calc")]
    public async Task<ActionResult<TrunkingCalcResponse>> Calculate([FromBody] TrunkingCalcRequest request)
    {
        var result = await _calcService.CalculateAsync(request);
        return Ok(result);
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await CatalogSeeder.ResetAsync(_context);
        return Ok(new { message = "線槽型錄已恢復預設" });
    }
}
