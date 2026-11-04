using Microsoft.AspNetCore.Authorization;

namespace Lalamog.Api.Requirements;

public class SystemFeatureRequirement : IAuthorizationRequirement
{
    public SystemFeatureRequirement(string code)
    {
        Code = code;
    }

    public string Code { get; }
}
