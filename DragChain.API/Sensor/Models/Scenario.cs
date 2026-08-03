using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("Scenarios")]
public class Scenario
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // s1, s2, ...

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string? Desc { get; set; }

    public int SortOrder { get; set; }

    public List<ScenarioFunction> Functions { get; set; } = [];
}

[Table("ScenarioFunctions")]
public class ScenarioFunction
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // f1, f2, ...

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int ScenarioId { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(ScenarioId))]
    public Scenario? Scenario { get; set; }

    public List<FunctionCondition> Conditions { get; set; } = [];
}

[Table("FunctionConditions")]
public class FunctionCondition
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // c1a, c1b, ...

    [Required]
    public string Name { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int FunctionId { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(FunctionId))]
    public ScenarioFunction? Function { get; set; }
}
