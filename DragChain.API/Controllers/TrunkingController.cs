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
    private const string FillRatioLimitKey = "TrunkingFillRatioLimit";
    private const decimal DefaultFillRatioLimit = 0.75m;

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

    [HttpGet("settings")]
    public async Task<ActionResult<TrunkingSettingsDto>> GetSettings()
    {
        var setting = await _context.AppSettings.FindAsync(FillRatioLimitKey);
        return new TrunkingSettingsDto
        {
            FillRatio = ParseFillRatio(setting?.Value)
        };
    }

    [HttpPut("settings")]
    public async Task<ActionResult<TrunkingSettingsDto>> UpdateSettings([FromBody] TrunkingSettingsDto dto)
    {
        if (dto.FillRatio <= 0 || dto.FillRatio > 1)
        {
            return BadRequest("填充率上限必须大于 0 且不超过 100%。");
        }

        var setting = await _context.AppSettings.FindAsync(FillRatioLimitKey);
        if (setting == null)
        {
            setting = new AppSetting { Key = FillRatioLimitKey };
            _context.AppSettings.Add(setting);
        }

        // 固定保存小数值，避免不同区域格式导致解析失败。
        setting.Value = dto.FillRatio.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await _context.SaveChangesAsync();

        return new TrunkingSettingsDto { FillRatio = dto.FillRatio };
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

    private static decimal ParseFillRatio(string? value)
    {
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var fillRatio)
            && fillRatio > 0
            && fillRatio <= 1
            ? fillRatio
            : DefaultFillRatioLimit;
    }
}
