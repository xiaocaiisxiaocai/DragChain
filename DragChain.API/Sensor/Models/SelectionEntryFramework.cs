using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("SelectionEntries")]
public class SelectionEntry
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public int SortOrder { get; set; }

    public List<EntryTreeNode> TreeNodes { get; set; } = [];
}

[Table("BusinessNodes")]
public class BusinessNode
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string NodeType { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string? Description { get; set; }
}

[Table("EntryTreeNodes")]
public class EntryTreeNode
{
    public int Id { get; set; }

    public int EntryId { get; set; }

    public int BusinessNodeId { get; set; }

    public int? ParentId { get; set; }

    public string? DisplayName { get; set; }

    public string? DescriptionOverride { get; set; }

    public int SortOrder { get; set; }

    public bool InheritRules { get; set; } = true;

    [ForeignKey(nameof(EntryId))]
    public SelectionEntry? Entry { get; set; }

    [ForeignKey(nameof(BusinessNodeId))]
    public BusinessNode? BusinessNode { get; set; }

    [ForeignKey(nameof(ParentId))]
    public EntryTreeNode? Parent { get; set; }

    public List<EntryTreeNode> Children { get; set; } = [];

    public List<RuleEntryBinding> RuleBindings { get; set; } = [];
}

[Table("RuleEntryBindings")]
public class RuleEntryBinding
{
    public int Id { get; set; }

    public int RuleId { get; set; }

    public int EntryTreeNodeId { get; set; }

    public bool IncludeDescendants { get; set; }

    public int SortOrder { get; set; }

    public string? Note { get; set; }

    [ForeignKey(nameof(RuleId))]
    public SelectionRule? Rule { get; set; }

    [ForeignKey(nameof(EntryTreeNodeId))]
    public EntryTreeNode? EntryTreeNode { get; set; }
}

[Table("SelectionResults")]
public class SelectionResult
{
    public int Id { get; set; }

    public int EntryTreeNodeId { get; set; }

    public string? Note { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(EntryTreeNodeId))]
    public EntryTreeNode? EntryTreeNode { get; set; }

    public List<SelectionResultProduct> Products { get; set; } = [];
}

[Table("SelectionResultProducts")]
public class SelectionResultProduct
{
    public int Id { get; set; }

    public int SelectionResultId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    [ForeignKey(nameof(SelectionResultId))]
    public SelectionResult? SelectionResult { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
