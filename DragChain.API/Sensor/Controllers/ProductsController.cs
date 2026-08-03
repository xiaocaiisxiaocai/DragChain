using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Data;
using DragChain.API.Sensor.Models;
using DragChain.API.Sensor.Services;

namespace DragChain.API.Sensor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly SensorDbContext _context;

    public ProductsController(SensorDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? type, [FromQuery] string? keyword)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(p =>
                (p.Code != null && p.Code.Contains(term)) ||
                p.Model.Contains(term) ||
                p.Name.Contains(term) ||
                p.Brand.Contains(term) ||
                (p.Spec != null && p.Spec.Contains(term)) ||
                (p.Scene != null && p.Scene.Contains(term)) ||
                _context.SensorTypes.Any(t => t.Id == p.Type && t.Name.Contains(term)));
        }
        return await query.OrderBy(p => p.Id).ToListAsync();
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportProducts([FromQuery] string? type)
    {
        var query = _context.Products.AsQueryable();
        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.Type == type);

        var products = await query.OrderBy(p => p.Id).ToListAsync();
        var typeNames = await _context.SensorTypes.ToDictionaryAsync(t => t.Id, t => t.Name);
        var bytes = ProductWorkbookService.CreateWorkbook(
            products,
            productType => typeNames.GetValueOrDefault(productType, productType));

        var fileName = $"products-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(bytes, ProductWorkbookService.ExcelContentType, fileName);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? NotFound() : Ok(product);
    }

    [HttpGet("{id}/replacement-preview")]
    public async Task<ActionResult<ProductReplacementPreviewDto>> GetReplacementPreview(
        int id,
        [FromQuery] int newProductId)
    {
        if (id == newProductId)
            return BadRequest(new { message = "新旧感应器不能相同" });

        var preview = await BuildReplacementPreviewAsync(id, newProductId);
        return preview == null ? NotFound() : Ok(preview);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        NormalizeProductCode(product);
        if (string.IsNullOrWhiteSpace(product.Code))
            return BadRequest(new { message = "请填写料号" });
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    [HttpPost("import")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ImportProducts([FromForm] IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "请选择要导入的 xlsx 文件" });

        await using var stream = file.OpenReadStream();
        var parseResult = ProductWorkbookService.ParseWorkbook(stream);
        if (parseResult.Errors.Count > 0)
            return BadRequest(new { message = "导入文件校验失败", errors = parseResult.Errors });

        var products = await _context.Products.ToListAsync();
        var sensorTypes = await _context.SensorTypes.ToListAsync();
        var typeByName = sensorTypes
            .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var byId = products.ToDictionary(p => p.Id);
        var byCode = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Code))
            .GroupBy(p => p.Code!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var byModel = products
            .Where(p => !string.IsNullOrWhiteSpace(p.Model))
            .GroupBy(p => p.Model.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var row in parseResult.Rows)
        {
            var typeText = row.Type.Trim();
            var productType = typeByName.GetValueOrDefault(typeText);
            if (string.IsNullOrWhiteSpace(productType))
            {
                errors.Add($"第 {row.RowNumber} 行类型「{typeText}」不存在，请使用系统已有类型名称");
                continue;
            }

            Product? product = null;
            if (row.Id.HasValue)
                byId.TryGetValue(row.Id.Value, out product);
            if (product == null && !string.IsNullOrWhiteSpace(row.Code))
                byCode.TryGetValue(row.Code.Trim(), out product);
            if (product == null)
                byModel.TryGetValue(row.Model.Trim(), out product);

            if (product == null)
            {
                product = new Product();
                _context.Products.Add(product);
                created++;
            }
            else
            {
                updated++;
            }

            // 导入以表格内容为准，同一文件可用于批量新增和覆盖维护。
            product.Code = NormalizeCode(row.Code);
            product.Model = row.Model.Trim();
            product.Name = row.Name.Trim();
            product.Brand = row.Brand.Trim();
            product.Type = productType;
            product.Spec = string.IsNullOrWhiteSpace(row.Spec) ? null : row.Spec.Trim();
            product.Scene = string.IsNullOrWhiteSpace(row.Scene) ? null : row.Scene.Trim();
        }

        if (errors.Count > 0)
            return BadRequest(new { message = "导入文件校验失败", errors });

        await _context.SaveChangesAsync();
        return Ok(new { created, updated, total = parseResult.Rows.Count });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        if (id != product.Id) return BadRequest();
        NormalizeProductCode(product);
        if (string.IsNullOrWhiteSpace(product.Code))
            return BadRequest(new { message = "请填写料号" });
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/replace-selection-results")]
    public async Task<ActionResult<ProductReplacementPreviewDto>> ReplaceSelectionResultProduct(
        int id,
        [FromBody] ReplaceSelectionResultProductDto dto)
    {
        if (dto.NewProductId <= 0)
            return BadRequest(new { message = "请选择新的感应器" });
        if (id == dto.NewProductId)
            return BadRequest(new { message = "新旧感应器不能相同" });

        var preview = await BuildReplacementPreviewAsync(id, dto.NewProductId);
        if (preview == null) return NotFound();

        var affected = await _context.SelectionResultProducts
            .Where(item => item.ProductId == id)
            .ToListAsync();
        if (affected.Count == 0) return Ok(preview);

        var affectedResultIds = affected.Select(item => item.SelectionResultId).Distinct().ToList();
        var resultProducts = await _context.SelectionResultProducts
            .Where(item => affectedResultIds.Contains(item.SelectionResultId))
            .ToListAsync();

        await using var transaction = await _context.Database.BeginTransactionAsync();
        foreach (var oldItem in affected)
        {
            var targetItem = resultProducts.FirstOrDefault(item =>
                item.SelectionResultId == oldItem.SelectionResultId &&
                item.ProductId == dto.NewProductId);

            if (targetItem == null)
            {
                oldItem.ProductId = dto.NewProductId;
                continue;
            }

            targetItem.Quantity = Math.Max(targetItem.Quantity, 1) + Math.Max(oldItem.Quantity, 1);
            _context.SelectionResultProducts.Remove(oldItem);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(preview);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var existing = await _context.Products.FindAsync(id);
        if (existing == null) return NotFound();
        var referenceCount = await _context.SelectionResultProducts.CountAsync(item => item.ProductId == id);
        if (referenceCount > 0)
            return Conflict(new { message = $"该感应器已被 {referenceCount} 条选型结果引用，请先使用一键替换或移除引用" });

        _context.Products.Remove(existing);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ProductReplacementPreviewDto?> BuildReplacementPreviewAsync(int oldProductId, int newProductId)
    {
        var oldProduct = await _context.Products.FindAsync(oldProductId);
        var newProduct = await _context.Products.FindAsync(newProductId);
        if (oldProduct == null || newProduct == null) return null;

        var resultRows = await _context.SelectionResultProducts
            .Where(item => item.ProductId == oldProductId)
            .Include(item => item.SelectionResult).ThenInclude(result => result!.EntryTreeNode)
            .ThenInclude(node => node!.BusinessNode)
            .ToListAsync();

        var affectedResultIds = resultRows.Select(item => item.SelectionResultId).Distinct().ToList();
        var targetRows = affectedResultIds.Count == 0
            ? new List<SelectionResultProduct>()
            : await _context.SelectionResultProducts
                .Where(item => affectedResultIds.Contains(item.SelectionResultId) && item.ProductId == newProductId)
                .ToListAsync();
        var targetByResultId = targetRows.ToDictionary(item => item.SelectionResultId);
        var mergeCount = affectedResultIds.Count == 0
            ? 0
            : targetByResultId.Count;

        var affectedNodes = resultRows
            .Select(item => new
            {
                NodeId = item.SelectionResult?.EntryTreeNodeId ?? 0,
                NodeName = item.SelectionResult?.EntryTreeNode?.DisplayName
                    ?? item.SelectionResult?.EntryTreeNode?.BusinessNode?.Name
                    ?? ""
            })
            .Where(item => item.NodeId > 0)
            .GroupBy(item => item.NodeId)
            .Select(group => new ProductReplacementNodePreviewDto
            {
                NodeId = group.Key,
                NodeName = string.IsNullOrWhiteSpace(group.First().NodeName) ? $"分类 {group.Key}" : group.First().NodeName,
                ResultCount = group.Count()
            })
            .OrderBy(item => item.NodeName)
            .ToList();

        var affectedDetails = resultRows
            .Select(item =>
            {
                targetByResultId.TryGetValue(item.SelectionResultId, out var targetItem);
                var oldQuantity = Math.Max(item.Quantity, 1);
                var existingNewQuantity = Math.Max(targetItem?.Quantity ?? 0, 0);
                return new ProductReplacementResultPreviewDto
                {
                    ResultId = item.SelectionResultId,
                    NodeId = item.SelectionResult?.EntryTreeNodeId ?? 0,
                    NodeName = item.SelectionResult?.EntryTreeNode?.DisplayName
                        ?? item.SelectionResult?.EntryTreeNode?.BusinessNode?.Name
                        ?? "",
                    Note = item.SelectionResult?.Note,
                    OldQuantity = oldQuantity,
                    ExistingNewQuantity = existingNewQuantity,
                    FinalNewQuantity = oldQuantity + existingNewQuantity,
                    WillMerge = targetItem != null
                };
            })
            .OrderBy(item => item.NodeName)
            .ThenBy(item => item.ResultId)
            .ToList();

        return new ProductReplacementPreviewDto
        {
            OldProduct = ProductSummaryDto.From(oldProduct),
            NewProduct = ProductSummaryDto.From(newProduct),
            AffectedResultCount = affectedResultIds.Count,
            AffectedNodeCount = affectedNodes.Count,
            MergeResultCount = mergeCount,
            AffectedNodes = affectedNodes,
            AffectedResults = affectedDetails
        };
    }

    private static void NormalizeProductCode(Product product)
    {
        product.Code = NormalizeCode(product.Code);
    }

    private static string NormalizeCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
    }
}

public class ReplaceSelectionResultProductDto
{
    public int NewProductId { get; set; }
}

public class ProductReplacementPreviewDto
{
    public ProductSummaryDto OldProduct { get; set; } = new();
    public ProductSummaryDto NewProduct { get; set; } = new();
    public int AffectedResultCount { get; set; }
    public int AffectedNodeCount { get; set; }
    public int MergeResultCount { get; set; }
    public List<ProductReplacementNodePreviewDto> AffectedNodes { get; set; } = [];
    public List<ProductReplacementResultPreviewDto> AffectedResults { get; set; } = [];
}

public class ProductReplacementNodePreviewDto
{
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public int ResultCount { get; set; }
}

public class ProductReplacementResultPreviewDto
{
    public int ResultId { get; set; }
    public int NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int OldQuantity { get; set; }
    public int ExistingNewQuantity { get; set; }
    public int FinalNewQuantity { get; set; }
    public bool WillMerge { get; set; }
}

public class ProductSummaryDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Model { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    public static ProductSummaryDto From(Product product) => new()
    {
        Id = product.Id,
        Code = product.Code,
        Model = product.Model,
        Name = product.Name,
        Type = product.Type
    };
}
