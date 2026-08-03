using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("ProcessScenarios")]
public class ProcessScenario
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty; // ps1, ps2, ...

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string? Desc { get; set; }

    public string? SopSource { get; set; }

    public string? Category { get; set; } // board_type for FPC

    public int SortOrder { get; set; }

    public List<AffectedMechanism> AffectedMechanisms { get; set; } = [];

    public List<UnaffectedMechanism> UnaffectedMechanisms { get; set; } = [];
}

[Table("AffectedMechanisms")]
public class AffectedMechanism
{
    public int Id { get; set; }

    public int ProcessScenarioId { get; set; }

    [Required]
    public string MechanismCode { get; set; } = string.Empty; // s1, s3, ...

    [Required]
    public string MechanismName { get; set; } = string.Empty;

    public string? ChangeDesc { get; set; }

    public string? ChangeDescDetail { get; set; }

    public string? ChangeDescDetail2 { get; set; }

    public string? InstallNote { get; set; }

    public string? Condition { get; set; }

    public string? RelatedConditions { get; set; } // comma-separated condition codes

    [ForeignKey(nameof(ProcessScenarioId))]
    public ProcessScenario? ProcessScenario { get; set; }
}

[Table("UnaffectedMechanisms")]
public class UnaffectedMechanism
{
    public int Id { get; set; }

    public int ProcessScenarioId { get; set; }

    [Required]
    public string MechanismCode { get; set; } = string.Empty;

    [ForeignKey(nameof(ProcessScenarioId))]
    public ProcessScenario? ProcessScenario { get; set; }
}
