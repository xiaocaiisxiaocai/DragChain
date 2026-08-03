using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/selection-entries")]
public class SelectionEntriesController : ControllerBase
{
    private readonly SensorDbContext _context;

    public SelectionEntriesController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult> GetEntries()
    {
        var entries = await _context.SelectionEntries
            .OrderBy(e => e.SortOrder)
            .Select(e => new
            {
                e.Id,
                e.Code,
                e.Name,
                e.Icon,
                e.Description,
                e.IsSystem,
                e.SortOrder,
                NodeCount = e.TreeNodes.Count
            })
            .ToListAsync();

        return Ok(entries);
    }

    [HttpGet("{code}/tree")]
    public async Task<ActionResult> GetEntryTree(string code)
    {
        var entry = await _context.SelectionEntries.FirstOrDefaultAsync(e => e.Code == code);
        if (entry == null) return NotFound();

        var nodes = await _context.EntryTreeNodes
            .Include(n => n.BusinessNode)
            .Where(n => n.EntryId == entry.Id)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

        var childrenMap = nodes
            .GroupBy(n => n.ParentId ?? 0)
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.SortOrder).ToList());

        return Ok(new
        {
            entry.Id,
            entry.Code,
            entry.Name,
            entry.Icon,
            entry.Description,
            Tree = BuildTree(0, childrenMap)
        });
    }

    private static List<object> BuildTree(int parentId, Dictionary<int, List<DragChain.API.Sensor.Models.EntryTreeNode>> childrenMap)
    {
        if (!childrenMap.TryGetValue(parentId, out var children)) return [];

        return children.Select(n => new
        {
            n.Id,
            n.EntryId,
            n.BusinessNodeId,
            n.ParentId,
            Code = n.BusinessNode?.Code ?? "",
            Name = n.DisplayName ?? n.BusinessNode?.Name ?? "",
            NodeType = n.BusinessNode?.NodeType ?? "",
            Icon = n.BusinessNode?.Icon ?? "",
            Description = n.DescriptionOverride ?? n.BusinessNode?.Description,
            n.SortOrder,
            n.InheritRules,
            Children = BuildTree(n.Id, childrenMap)
        }).Cast<object>().ToList();
    }

}
