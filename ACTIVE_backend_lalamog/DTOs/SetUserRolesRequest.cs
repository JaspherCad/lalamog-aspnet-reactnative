using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs;

public class SetUserRolesRequest
{
    [Required, MaxLength(100)]
    public List<int> RoleIds { get; set; } = null!;
}
