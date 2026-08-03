using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("Products")]
public class Product
{
    public int Id { get; set; }

    [Required]
    public string Code { get; set; } = string.Empty;

    [Required]
    public string Model { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty; // proximity_18, photoelectric-bg, etc.

    public string? Spec { get; set; }

    public string? Scene { get; set; }
}
