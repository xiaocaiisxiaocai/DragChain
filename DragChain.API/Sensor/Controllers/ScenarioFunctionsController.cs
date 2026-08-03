using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/scenario-functions")]
public class ScenarioFunctionsController : ControllerBase
{
    private readonly SensorDbContext _context;

    public ScenarioFunctionsController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetFunctions([FromQuery] int? scenarioId)
    {
        var query = _context.ScenarioFunctions.Include(f => f.Conditions).AsQueryable();
        if (scenarioId.HasValue)
            query = query.Where(f => f.ScenarioId == scenarioId.Value);
        var functions = await query.OrderBy(f => f.SortOrder).ToListAsync();
        return Ok(functions.Select(f => new
        {
            f.Id, f.Code, f.Name, f.Icon, f.Note, f.ScenarioId, f.SortOrder,
            Conditions = f.Conditions.OrderBy(c => c.SortOrder).Select(c => new
            {
                c.Id, c.Code, c.Name, c.Note, c.FunctionId, c.SortOrder
            })
        }));
    }

    [HttpPost]
    public async Task<ActionResult> CreateFunction([FromBody] CreateFunctionDto dto)
    {
        var function = new ScenarioFunction
        {
            Code = string.IsNullOrWhiteSpace(dto.Code) ? InternalCode.FromName(dto.Name, "function") : dto.Code,
            Name = dto.Name,
            Icon = dto.Icon,
            Note = dto.Note,
            ScenarioId = dto.ScenarioId,
            SortOrder = _context.ScenarioFunctions.Any() ? _context.ScenarioFunctions.Max(f => f.SortOrder) + 1 : 1
        };
        _context.ScenarioFunctions.Add(function);
        await _context.SaveChangesAsync();
        return Ok(function);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFunction(int id, [FromBody] UpdateFunctionDto dto)
    {
        var function = await _context.ScenarioFunctions.FindAsync(id);
        if (function == null) return NotFound();
        function.Name = dto.Name;
        function.Icon = dto.Icon;
        function.Note = dto.Note;
        function.ScenarioId = dto.ScenarioId;
        function.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFunction(int id)
    {
        var function = await _context.ScenarioFunctions.FindAsync(id);
        if (function == null) return NotFound();
        _context.ScenarioFunctions.Remove(function);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderFunctions([FromBody] List<ReorderItemDto> items)
    {
        foreach (var item in items)
        {
            var function = await _context.ScenarioFunctions.FindAsync(item.Id);
            if (function != null) function.SortOrder = item.SortOrder;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateFunctionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ScenarioId { get; set; }
}

public class UpdateFunctionDto
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int ScenarioId { get; set; }
    public int SortOrder { get; set; }
}
