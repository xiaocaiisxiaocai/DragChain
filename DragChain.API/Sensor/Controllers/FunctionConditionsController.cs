using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/function-conditions")]
public class FunctionConditionsController : ControllerBase
{
    private readonly SensorDbContext _context;

    public FunctionConditionsController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetConditions([FromQuery] int? functionId)
    {
        var query = _context.FunctionConditions.AsQueryable();
        if (functionId.HasValue)
            query = query.Where(c => c.FunctionId == functionId.Value);
        var conditions = await query.OrderBy(c => c.SortOrder).ToListAsync();
        return Ok(conditions.Select(c => new
        {
            c.Id, c.Code, c.Name, c.Note, c.FunctionId, c.SortOrder
        }));
    }

    [HttpPost]
    public async Task<ActionResult> CreateCondition([FromBody] CreateConditionDto dto)
    {
        var condition = new FunctionCondition
        {
            Code = string.IsNullOrWhiteSpace(dto.Code) ? InternalCode.FromName(dto.Name, "condition") : dto.Code,
            Name = dto.Name,
            Note = dto.Note,
            FunctionId = dto.FunctionId,
            SortOrder = _context.FunctionConditions.Any() ? _context.FunctionConditions.Max(c => c.SortOrder) + 1 : 1
        };
        _context.FunctionConditions.Add(condition);
        await _context.SaveChangesAsync();
        return Ok(condition);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCondition(int id, [FromBody] UpdateConditionDto dto)
    {
        var condition = await _context.FunctionConditions.FindAsync(id);
        if (condition == null) return NotFound();
        condition.Name = dto.Name;
        condition.Note = dto.Note;
        condition.FunctionId = dto.FunctionId;
        condition.SortOrder = dto.SortOrder;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCondition(int id)
    {
        var condition = await _context.FunctionConditions.FindAsync(id);
        if (condition == null) return NotFound();
        _context.FunctionConditions.Remove(condition);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderConditions([FromBody] List<ReorderItemDto> items)
    {
        foreach (var item in items)
        {
            var condition = await _context.FunctionConditions.FindAsync(item.Id);
            if (condition != null) condition.SortOrder = item.SortOrder;
        }
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public class CreateConditionDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int FunctionId { get; set; }
}

public class UpdateConditionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int FunctionId { get; set; }
    public int SortOrder { get; set; }
}
