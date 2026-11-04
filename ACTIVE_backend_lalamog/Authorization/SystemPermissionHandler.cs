using System.Security.Claims;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace Lalamog.Api.Handlers;

public class SystemPermissionHandler : AuthorizationHandler<SystemFeatureRequirement>
{
    private readonly IAccessService _access;

    public SystemPermissionHandler(IAccessService access)
    {
        _access = access;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, SystemFeatureRequirement requirement)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (context.User.Identity?.IsAuthenticated != true || !Guid.TryParse(userId, out Guid actorId))
        {
            return;
        }

        CancellationToken cancellationToken = CancellationToken.None;
        if (context.Resource is HttpContext httpContext)
        {
            cancellationToken = httpContext.RequestAborted;
        }
        if (await _access.HasFeatureAsync(actorId, requirement.Code, cancellationToken))
        {
            context.Succeed(requirement);
        }
    }
}
