using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DragChain.API.Sensor.Security;

namespace DragChain.API.Sensor.Models;

[Table("RbacUsers")]
public class RbacUser
{
    public int Id { get; set; }

    [Required]
    public string EmployeeNo { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string SecurityStamp { get; set; } = RbacPasswordHasher.CreateSecurityStamp();

    [Required]
    public string Role { get; set; } = "user";

    public bool Enabled { get; set; } = true;
}
