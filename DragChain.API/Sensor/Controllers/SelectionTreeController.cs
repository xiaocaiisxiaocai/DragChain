using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/selection-tree")]
public class SelectionTreeController : ControllerBase
{
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp"
    };

    private readonly SensorDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public SelectionTreeController(SensorDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    public async Task<ActionResult> GetTree()
    {
        var entry = await EnsureSelectorEntryAsync();
        var nodes = await _context.EntryTreeNodes
            .Include(n => n.BusinessNode)
            .Where(n => n.EntryId == entry.Id)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

        var childrenMap = nodes
            .GroupBy(n => n.ParentId ?? 0)
            .ToDictionary(group => group.Key, group => group.OrderBy(n => n.SortOrder).ToList());

        return Ok(BuildTree(0, childrenMap));
    }

    [HttpPost("nodes")]
    public async Task<ActionResult> CreateNode([FromBody] SaveSelectionNodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "分类名称不能为空" });

        var entry = await EnsureSelectorEntryAsync();
        if (dto.ParentId.HasValue)
        {
            var parent = await _context.EntryTreeNodes.FindAsync(dto.ParentId.Value);
            if (parent == null || parent.EntryId != entry.Id) return BadRequest(new { message = "父级分类不存在" });
        }

        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? $"selector:{Guid.NewGuid():N}"
            : dto.Code.Trim();
        var businessNode = new BusinessNode
        {
            Code = code,
            Name = dto.Name.Trim(),
            NodeType = string.IsNullOrWhiteSpace(dto.NodeType) ? "category" : dto.NodeType.Trim(),
            Icon = dto.Icon?.Trim() ?? "",
            Description = Normalize(dto.Description)
        };
        _context.BusinessNodes.Add(businessNode);
        await _context.SaveChangesAsync();

        var sortOrder = await NextSortOrderAsync(entry.Id, dto.ParentId);
        var treeNode = new EntryTreeNode
        {
            EntryId = entry.Id,
            BusinessNodeId = businessNode.Id,
            ParentId = dto.ParentId,
            DisplayName = dto.Name.Trim(),
            DescriptionOverride = Normalize(dto.Description),
            SortOrder = sortOrder,
            InheritRules = false
        };
        _context.EntryTreeNodes.Add(treeNode);
        await _context.SaveChangesAsync();
        return Ok(ToNodeDto(treeNode, businessNode, []));
    }

    [HttpPut("nodes/{id:int}")]
    public async Task<IActionResult> UpdateNode(int id, [FromBody] SaveSelectionNodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "分类名称不能为空" });

        var node = await _context.EntryTreeNodes
            .Include(n => n.BusinessNode)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (node == null || node.BusinessNode == null) return NotFound();

        node.DisplayName = dto.Name.Trim();
        node.DescriptionOverride = Normalize(dto.Description);
        node.SortOrder = dto.SortOrder;
        node.BusinessNode.Name = dto.Name.Trim();
        node.BusinessNode.Icon = dto.Icon?.Trim() ?? "";
        node.BusinessNode.Description = Normalize(dto.Description);
        node.BusinessNode.NodeType = string.IsNullOrWhiteSpace(dto.NodeType) ? "category" : dto.NodeType.Trim();
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("nodes/{id:int}")]
    public async Task<IActionResult> DeleteNode(int id)
    {
        var node = await _context.EntryTreeNodes
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (node == null) return NotFound();
        if (node.Children.Count > 0) return Conflict(new { message = "请先删除子分类" });
        var hasResults = await _context.SelectionResults.AnyAsync(result => result.EntryTreeNodeId == id);
        if (hasResults) return Conflict(new { message = "请先删除该分类下的选型结果" });

        var businessNode = node.BusinessNodeId > 0 ? await _context.BusinessNodes.FindAsync(node.BusinessNodeId) : null;
        if (businessNode != null)
        {
            var stillUsed = await _context.EntryTreeNodes
                .AnyAsync(item => item.Id != node.Id && item.BusinessNodeId == node.BusinessNodeId);
            if (!stillUsed) _context.BusinessNodes.Remove(businessNode);
        }

        _context.EntryTreeNodes.Remove(node);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("nodes/{nodeId:int}/results")]
    public async Task<ActionResult> GetNodeResults(int nodeId)
    {
        var exists = await _context.EntryTreeNodes.AnyAsync(n => n.Id == nodeId);
        if (!exists) return NotFound();

        var results = await _context.SelectionResults
            .Where(result => result.EntryTreeNodeId == nodeId)
            .Include(result => result.EntryTreeNode).ThenInclude(node => node!.BusinessNode)
            .Include(result => result.Products).ThenInclude(item => item.Product)
            .OrderBy(result => result.SortOrder)
            .ToListAsync();

        return Ok(results.Select(ToResultDto));
    }

    [HttpGet("results/search")]
    public async Task<ActionResult> SearchResults([FromQuery] string? keyword)
    {
        var term = Normalize(keyword);
        var query = _context.SelectionResults
            .Include(result => result.EntryTreeNode).ThenInclude(node => node!.BusinessNode)
            .Include(result => result.Products).ThenInclude(item => item.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            query = query.Where(result =>
                (result.Note != null && result.Note.Contains(term)) ||
                (result.EntryTreeNode != null &&
                    ((result.EntryTreeNode.DisplayName != null && result.EntryTreeNode.DisplayName.Contains(term)) ||
                     (result.EntryTreeNode.BusinessNode != null && result.EntryTreeNode.BusinessNode.Name.Contains(term)))) ||
                result.Products.Any(item =>
                    item.Product != null &&
                    ((item.Product.Model != null && item.Product.Model.Contains(term)) ||
                     item.Product.Name.Contains(term) ||
                     _context.SensorTypes.Any(type => type.Id == item.Product.Type && type.Name.Contains(term)))));
        }

        var results = await query
            .OrderBy(result => result.EntryTreeNode!.SortOrder)
            .ThenBy(result => result.SortOrder)
            .Take(100)
            .ToListAsync();

        return Ok(results.Select(ToResultDto));
    }

    [HttpPost("nodes/{nodeId:int}/results")]
    public async Task<ActionResult> CreateNodeResult(int nodeId, [FromBody] SaveSelectionResultDto dto)
    {
        var node = await _context.EntryTreeNodes.FindAsync(nodeId);
        if (node == null) return NotFound();
        var productBuild = await BuildResultProductsAsync(dto.Products);
        if (productBuild.Error != null) return productBuild.Error;

        var result = new SelectionResult
        {
            EntryTreeNodeId = node.Id,
            Note = Normalize(dto.Note),
            SortOrder = await NextResultSortOrderAsync(node.Id),
            Products = productBuild.Products
        };
        _context.SelectionResults.Add(result);
        await _context.SaveChangesAsync();
        return Ok(ToResultDto(result));
    }

    [HttpPut("results/{resultId:int}")]
    public async Task<IActionResult> UpdateResult(int resultId, [FromBody] SaveSelectionResultDto dto)
    {
        var result = await _context.SelectionResults
            .Include(item => item.Products)
            .FirstOrDefaultAsync(item => item.Id == resultId);
        if (result == null) return NotFound();
        var productBuild = await BuildResultProductsAsync(dto.Products, result.Id);
        if (productBuild.Error != null) return productBuild.Error;

        result.Note = Normalize(dto.Note);
        result.SortOrder = dto.SortOrder;
        _context.SelectionResultProducts.RemoveRange(result.Products);
        result.Products = productBuild.Products;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("results/{resultId:int}/image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult> UploadResultImage(int resultId, [FromForm] IFormFile file)
    {
        var result = await _context.SelectionResults.FirstOrDefaultAsync(result => result.Id == resultId);
        if (result == null) return NotFound();
        if (file.Length == 0) return BadRequest(new { message = "请选择图片" });

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
            return BadRequest(new { message = "只支持 png、jpg、jpeg、webp 图片" });

        var storageRoot = GetImageStorageRoot();
        var storageDirectory = GetNodeImageStorageDirectory(result.EntryTreeNodeId);
        Directory.CreateDirectory(storageDirectory);
        DeleteResultImages(resultId, result.EntryTreeNodeId);

        var relativePath = Path.Combine(result.EntryTreeNodeId.ToString(), $"{resultId}{extension.ToLowerInvariant()}");
        var fullPath = Path.Combine(storageRoot, relativePath);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { ImageUrl = BuildImageUrl(relativePath, System.IO.File.GetLastWriteTimeUtc(fullPath)) });
    }

    [HttpDelete("results/{resultId:int}/image")]
    public async Task<IActionResult> DeleteResultImage(int resultId)
    {
        var result = await _context.SelectionResults.FirstOrDefaultAsync(result => result.Id == resultId);
        if (result == null) return NotFound();

        DeleteResultImages(resultId, result.EntryTreeNodeId);
        return NoContent();
    }

    [HttpDelete("results/{resultId:int}")]
    public async Task<IActionResult> DeleteResult(int resultId)
    {
        var result = await _context.SelectionResults.FindAsync(resultId);
        if (result == null) return NotFound();
        var nodeId = result.EntryTreeNodeId;
        _context.SelectionResults.Remove(result);
        await _context.SaveChangesAsync();
        DeleteResultImages(resultId, nodeId);
        return NoContent();
    }

    private async Task<SelectionEntry> EnsureSelectorEntryAsync()
    {
        var entry = await _context.SelectionEntries.FirstOrDefaultAsync(e => e.Code == "selector");
        if (entry != null) return entry;

        entry = new SelectionEntry
        {
            Code = "selector",
            Name = "选型配置",
            Icon = "",
            Description = "选型配置分类树",
            IsSystem = true,
            SortOrder = 0
        };
        _context.SelectionEntries.Add(entry);
        await _context.SaveChangesAsync();
        return entry;
    }

    private async Task<int> NextSortOrderAsync(int entryId, int? parentId)
    {
        var siblings = _context.EntryTreeNodes.Where(n => n.EntryId == entryId && n.ParentId == parentId);
        return await siblings.AnyAsync() ? await siblings.MaxAsync(n => n.SortOrder) + 1 : 1;
    }

    private async Task<int> NextResultSortOrderAsync(int nodeId)
    {
        var results = _context.SelectionResults.Where(n => n.EntryTreeNodeId == nodeId);
        return await results.AnyAsync() ? await results.MaxAsync(n => n.SortOrder) + 1 : 1;
    }

    private async Task<ResultProductBuildResult> BuildResultProductsAsync(
        List<SelectionResultProductDto> dtoProducts,
        int? selectionResultId = null)
    {
        var normalized = dtoProducts
            .Where(item => item.ProductId > 0)
            .Select(item => new SelectionResultProductDto
            {
                ProductId = item.ProductId,
                Quantity = Math.Max(item.Quantity, 1)
            })
            .ToList();
        if (normalized.Count == 0)
            return ResultProductBuildResult.Succeeded([]);

        var duplicatedProductId = normalized
            .GroupBy(item => item.ProductId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;
        if (duplicatedProductId.HasValue)
            return ResultProductBuildResult.Failed(BadRequest(new { message = "同一条选型结果不能重复选择同一款感应器" }));

        var productIds = normalized.Select(item => item.ProductId).ToArray();
        var existingIds = await _context.Products
            .Where(product => productIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync();
        var missingIds = productIds.Except(existingIds).ToArray();
        if (missingIds.Length > 0)
            return ResultProductBuildResult.Failed(BadRequest(new { message = $"感应器不存在：{string.Join(", ", missingIds)}" }));

        return ResultProductBuildResult.Succeeded(normalized
            .Select(item => new SelectionResultProduct
            {
                SelectionResultId = selectionResultId ?? 0,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })
            .ToList());
    }

    private static List<object> BuildTree(int parentId, Dictionary<int, List<EntryTreeNode>> childrenMap)
    {
        if (!childrenMap.TryGetValue(parentId, out var children)) return [];

        return children
            .Select(node => ToNodeDto(node, node.BusinessNode, BuildTree(node.Id, childrenMap)))
            .Cast<object>()
            .ToList();
    }

    private static object ToNodeDto(EntryTreeNode node, BusinessNode? businessNode, List<object> children)
    {
        return new
        {
            node.Id,
            node.EntryId,
            node.ParentId,
            Code = businessNode?.Code ?? "",
            Name = node.DisplayName ?? businessNode?.Name ?? "",
            NodeType = businessNode?.NodeType ?? "",
            Icon = businessNode?.Icon ?? "",
            Description = node.DescriptionOverride ?? businessNode?.Description,
            node.SortOrder,
            IsLeaf = children.Count == 0,
            Children = children
        };
    }

    private object ToResultDto(SelectionResult result)
    {
        return new
        {
            result.Id,
            NodeId = result.EntryTreeNodeId,
            NodeName = result.EntryTreeNode?.DisplayName ?? result.EntryTreeNode?.BusinessNode?.Name ?? "",
            result.Note,
            result.SortOrder,
            ImageUrl = GetResultImageUrl(result),
            Products = result.Products.Select(product => new
            {
                ProductId = product.ProductId,
                ProductCode = product.Product?.Code ?? "",
                ProductModel = product.Product?.Model ?? "",
                ProductName = product.Product?.Name ?? "",
                ProductType = product.Product?.Type ?? "",
                product.Quantity
            })
        };
    }

    private string GetImageStorageRoot() =>
        Path.Combine(_environment.ContentRootPath, "storage", "selection-result-images");

    private string GetNodeImageStorageDirectory(int nodeId) =>
        Path.Combine(GetImageStorageRoot(), nodeId.ToString());

    private string? GetResultImageUrl(SelectionResult result)
    {
        var storageRoot = GetImageStorageRoot();
        if (!Directory.Exists(storageRoot)) return null;
        var imagePath = FindResultImagePath(storageRoot, result.EntryTreeNodeId, result.Id);
        if (imagePath == null) return null;

        return BuildImageUrl(Path.GetRelativePath(storageRoot, imagePath), System.IO.File.GetLastWriteTimeUtc(imagePath));
    }

    private static string BuildImageUrl(string relativePath, DateTime updatedAt)
    {
        var urlPath = string.Join('/',
            relativePath
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
                .Select(Uri.EscapeDataString));
        return $"/selection-result-images/{urlPath}?v={updatedAt.Ticks}";
    }

    private static string? FindResultImagePath(string storageRoot, int nodeId, int resultId)
    {
        var nodeImagePath = FindResultImageInDirectory(Path.Combine(storageRoot, nodeId.ToString()), resultId);
        if (nodeImagePath != null) return nodeImagePath;

        var legacyImagePath = FindResultImageInDirectory(storageRoot, resultId);
        if (legacyImagePath != null) return legacyImagePath;

        return Directory
            .EnumerateDirectories(storageRoot)
            .Select(directory => FindResultImageInDirectory(directory, resultId))
            .FirstOrDefault(imagePath => imagePath != null);
    }

    private static string? FindResultImageInDirectory(string directory, int resultId)
    {
        if (!Directory.Exists(directory)) return null;
        return Directory
            .EnumerateFiles(directory, $"{resultId}.*")
            .FirstOrDefault(file => AllowedImageExtensions.Contains(Path.GetExtension(file)));
    }

    private void DeleteResultImages(int resultId, int nodeId)
    {
        var storageRoot = GetImageStorageRoot();
        if (!Directory.Exists(storageRoot)) return;

        var directories = new[] { storageRoot, Path.Combine(storageRoot, nodeId.ToString()) }
            .Concat(Directory.EnumerateDirectories(storageRoot))
            .Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            foreach (var imagePath in Directory.EnumerateFiles(directory, $"{resultId}.*"))
            {
                if (AllowedImageExtensions.Contains(Path.GetExtension(imagePath)))
                    System.IO.File.Delete(imagePath);
            }
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class ResultProductBuildResult
{
    private ResultProductBuildResult(List<SelectionResultProduct> products, ActionResult? error)
    {
        Products = products;
        Error = error;
    }

    public List<SelectionResultProduct> Products { get; }
    public ActionResult? Error { get; }

    public static ResultProductBuildResult Succeeded(List<SelectionResultProduct> products) => new(products, null);

    public static ResultProductBuildResult Failed(ActionResult error) => new([], error);
}

public class SaveSelectionNodeDto
{
    public int? ParentId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NodeType { get; set; }
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 1;
}

public class SaveSelectionResultDto
{
    public string? Note { get; set; }
    public int SortOrder { get; set; } = 1;
    public List<SelectionResultProductDto> Products { get; set; } = [];
}

public class SelectionResultProductDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
