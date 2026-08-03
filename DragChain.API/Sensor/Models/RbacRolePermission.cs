using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragChain.API.Sensor.Models;

[Table("RbacRolePermissions")]
public class RbacRolePermission
{
    public int Id { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string PermissionCode { get; set; } = string.Empty;
}
