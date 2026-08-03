using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("ProcessNotes")]
public class ProcessNote
{
    public int Id { get; set; }

    [Required]
    public string ProcessName { get; set; } = string.Empty;

    public string Characteristic { get; set; } = string.Empty;

    public string SelectionNote { get; set; } = string.Empty;
}
