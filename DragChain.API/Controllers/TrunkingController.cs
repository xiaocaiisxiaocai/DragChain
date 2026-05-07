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
    public async Task<ActionResult<IEnumerable<TrunkingCatalogDto>>> GetAll()
    {
        var rows = await _context.TrunkingCatalog
            .OrderBy(t => t.Id)
            .Select(t => new TrunkingCatalogDto
            {
                Id = t.Id,
                Model = t.Model,
                Width = t.Width,
                Height = t.Height,
                CrossSection = t.CrossSection
            })
            .ToListAsync();
        return rows;
    }

    [HttpPost]
    public async Task<ActionResult<TrunkingCatalogDto>> Create([FromBody] CreateTrunkingCatalogDto dto)
    {
        var maxId = await _context.TrunkingCatalog.MaxAsync(t => (int?)t.Id) ?? 0;
        var tk = new TrunkingCatalog
        {
            Id = maxId + 1,
            Model = dto.Model,
            Width = dto.Width,
            Height = dto.Height,
            CrossSection = dto.CrossSection
        };
        _context.TrunkingCatalog.Add(tk);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), new TrunkingCatalogDto
        {
            Id = tk.Id,
            Model = tk.Model,
            Width = tk.Width,
            Height = tk.Height,
            CrossSection = tk.CrossSection
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTrunkingCatalogDto dto)
    {
        var tk = await _context.TrunkingCatalog.FindAsync(id);
        if (tk == null) return NotFound();

        tk.Model = dto.Model;
        tk.Width = dto.Width;
        tk.Height = dto.Height;
        tk.CrossSection = dto.CrossSection;

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
