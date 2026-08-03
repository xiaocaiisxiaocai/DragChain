using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private const string SavedSelectionKey = "TrunkingSavedSelection";
    private const string SavedSelectionsKey = "TrunkingSavedSelections";
    private const decimal DefaultFillRatioLimit = 0.60m;
    private static readonly JsonSerializerOptions SavedSelectionJsonOptions = new(JsonSerializerDefaults.Web);

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
                CrossSection = t.CrossSection,
                FillRatioLimit = NormalizeFillRatio(t.FillRatioLimit)
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
            return BadRequest("有效利用率上限必须大于 0 且不超过 100%。");
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

    [HttpGet("saved-selection")]
    public async Task<ActionResult<TrunkingSavedSelectionDto?>> GetSavedSelection()
    {
        return (await LoadSavedSelectionsAsync()).OrderByDescending(item => item.SavedAt).FirstOrDefault();
    }

    [HttpGet("saved-selections")]
    public async Task<ActionResult<List<TrunkingSavedSelectionDto>>> GetSavedSelections()
    {
        return (await LoadSavedSelectionsAsync()).OrderByDescending(item => item.SavedAt).ToList();
    }

    [HttpPut("saved-selection")]
    public async Task<ActionResult<TrunkingSavedSelectionDto>> SaveSelection([FromBody] TrunkingSavedSelectionDto dto)
    {
        if (dto.Request.Slots.Count == 0)
        {
            return BadRequest("请先添加槽位后再保存选型。");
        }

        var saved = new TrunkingSavedSelectionDto
        {
            Id = string.IsNullOrWhiteSpace(dto.Id) ? Guid.NewGuid().ToString("N") : dto.Id.Trim(),
            Name = string.IsNullOrWhiteSpace(dto.Name) ? "线槽选型" : dto.Name.Trim(),
            SavedAt = DateTime.Now,
            Request = dto.Request,
            Result = await _calcService.CalculateAsync(dto.Request),
            SourceSlots = dto.SourceSlots
        };

        var selections = await LoadSavedSelectionsAsync();
        selections.RemoveAll(item => item.Id == saved.Id);
        selections.Add(saved);
        await SaveSelectionsAsync(selections);

        return saved;
    }

    [HttpGet("saved-selections/{id}")]
    public async Task<ActionResult<TrunkingSavedSelectionDto>> GetSavedSelectionById(string id)
    {
        var saved = (await LoadSavedSelectionsAsync()).FirstOrDefault(item => item.Id == id);
        return saved == null ? NotFound() : saved;
    }

    [HttpDelete("saved-selections/{id}")]
    public async Task<IActionResult> DeleteSavedSelection(string id)
    {
        var selections = await LoadSavedSelectionsAsync();
        var removed = selections.RemoveAll(item => item.Id == id);
        if (removed == 0) return NotFound();

        await SaveSelectionsAsync(selections);
        return NoContent();
    }

    private async Task<List<TrunkingSavedSelectionDto>> LoadSavedSelectionsAsync()
    {
        var listSetting = await _context.AppSettings.FindAsync(SavedSelectionsKey);
        var selections = DeserializeSavedSelections(listSetting?.Value);
        if (selections.Count > 0) return selections;

        var legacySetting = await _context.AppSettings.FindAsync(SavedSelectionKey);
        var legacy = DeserializeLegacySavedSelection(legacySetting?.Value);
        return legacy == null ? [] : [legacy];
    }

    private async Task SaveSelectionsAsync(List<TrunkingSavedSelectionDto> selections)
    {
        var value = JsonSerializer.Serialize(
            selections.OrderByDescending(item => item.SavedAt).ToList(),
            SavedSelectionJsonOptions);
        var setting = await _context.AppSettings.FindAsync(SavedSelectionsKey);
        if (setting == null)
        {
            setting = new AppSetting { Key = SavedSelectionsKey };
            _context.AppSettings.Add(setting);
        }

        setting.Value = value;

        var latest = selections.OrderByDescending(item => item.SavedAt).FirstOrDefault();
        var legacySetting = await _context.AppSettings.FindAsync(SavedSelectionKey);
        if (latest != null)
        {
            if (legacySetting == null)
            {
                legacySetting = new AppSetting { Key = SavedSelectionKey };
                _context.AppSettings.Add(legacySetting);
            }

            legacySetting.Value = JsonSerializer.Serialize(latest, SavedSelectionJsonOptions);
        }
        else if (legacySetting != null)
        {
            legacySetting.Value = string.Empty;
        }

        await _context.SaveChangesAsync();
    }

    private static List<TrunkingSavedSelectionDto> DeserializeSavedSelections(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<TrunkingSavedSelectionDto>>(value, SavedSelectionJsonOptions)
                ?.Where(item => item.Request.Slots.Count > 0)
                .Select(EnsureSavedSelectionId)
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static TrunkingSavedSelectionDto? DeserializeLegacySavedSelection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            var saved = JsonSerializer.Deserialize<TrunkingSavedSelectionDto>(value, SavedSelectionJsonOptions);
            return saved?.Request.Slots.Count > 0 ? EnsureSavedSelectionId(saved) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TrunkingSavedSelectionDto EnsureSavedSelectionId(TrunkingSavedSelectionDto saved)
    {
        if (string.IsNullOrWhiteSpace(saved.Id))
        {
            saved.Id = CreateStableSavedSelectionId(saved);
        }

        return saved;
    }

    private static string CreateStableSavedSelectionId(TrunkingSavedSelectionDto saved)
    {
        var fingerprint = JsonSerializer.Serialize(new
        {
            saved.Name,
            saved.SavedAt,
            SlotIds = saved.Request.Slots.Select(slot => slot.Id).ToList()
        }, SavedSelectionJsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))[..32].ToLowerInvariant();
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
            CrossSection = dto.CrossSection,
            FillRatioLimit = NormalizeFillRatio(dto.FillRatioLimit)
        };
        _context.TrunkingCatalog.Add(tk);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), MapToDto(tk));
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
        tk.FillRatioLimit = NormalizeFillRatio(dto.FillRatioLimit);

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

    private static decimal NormalizeFillRatio(decimal fillRatio)
    {
        return fillRatio > 0 && fillRatio <= 1
            ? fillRatio
            : DefaultFillRatioLimit;
    }

    private static TrunkingCatalogDto MapToDto(TrunkingCatalog tk) => new()
    {
        Id = tk.Id,
        Model = tk.Model,
        Width = tk.Width,
        Height = tk.Height,
        CrossSection = tk.CrossSection,
        FillRatioLimit = NormalizeFillRatio(tk.FillRatioLimit)
    };
}
