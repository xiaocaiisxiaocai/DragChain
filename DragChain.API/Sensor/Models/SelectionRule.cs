using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("SelectionRules")]
public class SelectionRule
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // r1001, r2001, ...

    public int ScenarioId { get; set; }

    public int FunctionId { get; set; }

    public int ConditionId { get; set; }

    public string? Note { get; set; }

    [ForeignKey(nameof(ScenarioId))]
    public Scenario? Scenario { get; set; }

    [ForeignKey(nameof(FunctionId))]
    public ScenarioFunction? Function { get; set; }

    [ForeignKey(nameof(ConditionId))]
    public FunctionCondition? Condition { get; set; }

    public List<RuleProduct> RuleProducts { get; set; } = [];
}

[Table("RuleProducts")]
public class RuleProduct
{
    public int Id { get; set; }

    public int RuleId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; } = 1;

    [ForeignKey(nameof(RuleId))]
    public SelectionRule? Rule { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }
}
