using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/process-notes")]
public class ProcessNotesController : ControllerBase
{
    private readonly SensorDbContext _context;

    public ProcessNotesController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProcessNote>>> GetProcessNotes([FromQuery] string? keyword)
    {
        var query = _context.ProcessNotes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(note =>
                note.ProcessName.Contains(term) ||
                note.Characteristic.Contains(term) ||
                note.SelectionNote.Contains(term));
        }
        return await query.OrderBy(note => note.Id).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProcessNote>> GetProcessNote(int id)
    {
        var note = await _context.ProcessNotes.FindAsync(id);
        return note == null ? NotFound() : Ok(note);
    }

    [HttpPost]
    public async Task<ActionResult<ProcessNote>> CreateProcessNote(ProcessNote note)
    {
        Normalize(note);
        if (string.IsNullOrWhiteSpace(note.ProcessName))
            return BadRequest(new { message = "制程名不能为空" });

        _context.ProcessNotes.Add(note);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProcessNote), new { id = note.Id }, note);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProcessNote(int id, ProcessNote note)
    {
        if (id != note.Id) return BadRequest();

        var existing = await _context.ProcessNotes.FindAsync(id);
        if (existing == null) return NotFound();

        Normalize(note);
        if (string.IsNullOrWhiteSpace(note.ProcessName))
            return BadRequest(new { message = "制程名不能为空" });

        existing.ProcessName = note.ProcessName;
        existing.Characteristic = note.Characteristic;
        existing.SelectionNote = note.SelectionNote;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProcessNote(int id)
    {
        var existing = await _context.ProcessNotes.FindAsync(id);
        if (existing == null) return NotFound();

        _context.ProcessNotes.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static void Normalize(ProcessNote note)
    {
        note.ProcessName = note.ProcessName.Trim();
        note.Characteristic = note.Characteristic.Trim();
        note.SelectionNote = note.SelectionNote.Trim();
    }
}
