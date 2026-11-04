using Lalamog.Api.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lalamog.Api.Controllers;

[Route("api/access")]
[Authorize(Policy = ApplicationPermissions.Admin_ManageAccess)]
public class AccessController : ApiController
{
    private readonly IAccessService _access;

    public AccessController(IAccessService access)
    {
        _access = access;
    }

    [HttpGet("system-features")]
    public async Task<ActionResult<List<SystemFeatureResponse>>> Features(CancellationToken cancellationToken)
    {
        return Ok(await _access.ListFeaturesAsync(cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleResponse>>> Roles(CancellationToken cancellationToken)
    {
        return Ok(await _access.ListRolesAsync(cancellationToken));
    }

    [HttpGet("users/{userId:guid}/roles")]
    public async Task<ActionResult<UserRolesResponse>> UserRoles(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _access.GetUserRolesAsync(userId, cancellationToken));
    }

    [HttpPut("users/{userId:guid}/roles")]
    public async Task<IActionResult> SetUserRoles(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        await _access.SetUserRolesAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("roles/{roleId:int}/system-features")]
    public async Task<IActionResult> SetRoleFeatures(int roleId, SetRoleSystemFeaturesRequest request, CancellationToken cancellationToken)
    {
        await _access.SetRoleFeaturesAsync(roleId, request, cancellationToken);
        return NoContent();
    }
}
