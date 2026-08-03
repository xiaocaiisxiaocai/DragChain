using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScenariosController : ControllerBase
{
    private readonly SensorDbContext _context;

    public ScenariosController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetScenarios()
    {
        var scenarios = await _context.Scenarios
            .Include(s => s.Functions).ThenInclude(f => f.Conditions)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();

        return Ok(scenarios.Select(s => new
        {
            s.Id, s.Code, s.Name, s.Icon, s.Desc,
            Functions = s.Functions.OrderBy(f => f.SortOrder).Select(f => new
            {
                f.Id, f.Code, f.Name, f.Icon, f.Note,
                Conditions = f.Conditions.OrderBy(c => c.SortOrder).Select(c => new
                {
                    c.Id, c.Code, c.Name, c.Note
                })
            })
        }));
    }

    [HttpPost]
    public async Task<ActionResult> CreateScenario([FromBody] CreateScenarioDto dto)
    {
        var scenario = new Scenario
        {
            Code = string.IsNullOrWhiteSpace(dto.Code) ? InternalCode.FromName(dto.Name, "scenario") : dto.Code,
            Name = dto.Name,
            Icon = dto.Icon,
            Desc = dto.Desc,
            SortOrder = _context.Scenarios.Any() ? _context.Scenarios.Max(s => s.SortOrder) + 1 : 1
        };
        _context.Scenarios.Add(scenario);
        await _context.SaveChangesAsync();
        return Ok(scenario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateScenario(int id, [FromBody] UpdateScenarioDto dto)
    {
        var scenario = await _context.Scenarios.FindAsync(id);
        if (scenario == null) return NotFound();
        scenario.Name = dto.Name;
        scenario.Icon = dto.Icon;
        scenario.Desc = dto.Desc;
        scenario.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScenario(int id)
    {
        var scenario = await _context.Scenarios.FindAsync(id);
        if (scenario == null) return NotFound();
        _context.Scenarios.Remove(scenario);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderScenarios([FromBody] List<ReorderItemDto> items)
    {
        foreach (var item in items)
        {
            var scenario = await _context.Scenarios.FindAsync(item.Id);
            if (scenario != null) scenario.SortOrder = item.SortOrder;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/sensor-types")]
public class SensorTypesController : ControllerBase
{
    private readonly SensorDbContext _context;

    public SensorTypesController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorType>>> GetSensorTypes([FromQuery] string? keyword)
    {
        var query = _context.SensorTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(type => type.Name.Contains(term));
        }
        return await query.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<SensorType>> CreateSensorType(SensorType sensorType)
    {
        if (string.IsNullOrWhiteSpace(sensorType.Id))
            sensorType.Id = InternalCode.FromName(sensorType.Name, "type");

        _context.SensorTypes.Add(sensorType);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSensorTypes), new { id = sensorType.Id }, sensorType);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSensorType(string id, SensorType sensorType)
    {
        if (id != sensorType.Id) return BadRequest();
        var existing = await _context.SensorTypes.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Name = sensorType.Name;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSensorType(string id)
    {
        var existing = await _context.SensorTypes.FindAsync(id);
        if (existing == null) return NotFound();
        var hasProducts = await _context.Products.AnyAsync(p => p.Type == id);
        if (hasProducts) return Conflict(new { message = "该类型下还有产品，无法删除" });
        _context.SensorTypes.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/process-scenarios")]
public class ProcessScenariosController : ControllerBase
{
    private readonly SensorDbContext _context;

    public ProcessScenariosController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetProcessScenarios()
    {
        var scenarios = await _context.ProcessScenarios
            .Include(ps => ps.AffectedMechanisms)
            .Include(ps => ps.UnaffectedMechanisms)
            .OrderBy(ps => ps.SortOrder)
            .ToListAsync();

        return Ok(scenarios.Select(ps => new
        {
            ps.Id, ps.Code, ps.Name, ps.Icon, ps.Desc, ps.SopSource, ps.Category,
            AffectedMechanisms = ps.AffectedMechanisms.Select(am => new
            {
                am.Id, am.MechanismCode, am.MechanismName,
                am.ChangeDesc, am.ChangeDescDetail, am.ChangeDescDetail2,
                am.InstallNote, am.Condition, am.RelatedConditions
            }),
            UnaffectedMechanisms = ps.UnaffectedMechanisms.Select(um => new
            {
                um.Id, um.MechanismCode
            })
        }));
    }
}

public class CreateScenarioDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Desc { get; set; }
}

public class UpdateScenarioDto
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Desc { get; set; }
    public int SortOrder { get; set; }
}

public class ReorderItemDto
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
}
