using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;

namespace Lalamog.Api.Services;

public class AccessService : IAccessService
{
    private readonly IAccessRepository _access;

    public AccessService(IAccessRepository access)
    {
        _access = access;
    }

    public Task<AuthResponse> SessionAsync(Guid userId, CancellationToken cancellationToken)
    { return _access.SessionAsync(userId, cancellationToken); }
    public Task EnableFighterAsync(Guid userId, CancellationToken cancellationToken)
    { return _access.EnableFighterAsync(userId, cancellationToken); }
    public Task<List<AccessUserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    { return _access.ListUsersAsync(cancellationToken); }

    public Task<bool> HasVisitorFeatureAsync(string code, CancellationToken cancellationToken)
    { return _access.HasVisitorFeatureAsync(code, cancellationToken); }

    public Task<bool> HasFeatureAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        return _access.HasFeatureAsync(userId, code, cancellationToken);
    }

    public async Task<List<SystemFeatureResponse>> ListFeaturesAsync(CancellationToken cancellationToken)
    {
        List<SystemFeature> features = await _access.ListFeaturesAsync(cancellationToken);
        var response = new List<SystemFeatureResponse>();
        foreach (SystemFeature feature in features)
        {
            response.Add(MapFeature(feature));
        }
        return response;
    }

    public async Task<List<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken)
    {
        List<Role> roles = await _access.ListRolesAsync(cancellationToken);
        var response = new List<RoleResponse>();
        foreach (Role role in roles)
        {
            var item = new RoleResponse { Id = role.Id, Name = role.Name, Description = role.Description };
            var grants = from grant in role.RoleSystemFeatures orderby grant.SystemFeature.Code select grant;
            foreach (RoleSystemFeature grant in grants)
            {
                item.SystemFeatures.Add(MapFeature(grant.SystemFeature));
            }
            response.Add(item);
        }
        return response;
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<int>? roleIds = await _access.GetUserRoleIdsAsync(userId, cancellationToken);
        if (roleIds == null)
        {
            throw new ApiException(404, "User not found.");
        }
        return new UserRolesResponse { UserId = userId, RoleIds = roleIds };
    }

    public async Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken)
    {
        List<int> roleIds = ValidateSelection(request.RoleIds);
        if (!await _access.RoleIdsExistAsync(roleIds, cancellationToken))
        {
            throw new ApiException(400, "Select existing roles.");
        }
        if (!await _access.SetUserRolesAsync(userId, roleIds, cancellationToken))
        {
            throw new ApiException(404, "User not found.");
        }
    }

    public async Task SetRoleFeaturesAsync(int roleId, SetRoleSystemFeaturesRequest request, CancellationToken cancellationToken)
    {
        List<int> featureIds = ValidateSelection(request.SystemFeatureIds);
        if (!await _access.FeatureIdsExistAsync(featureIds, cancellationToken))
        {
            throw new ApiException(400, "Select existing system features.");
        }
        if (!await _access.SetRoleFeaturesAsync(roleId, featureIds, cancellationToken))
        {
            throw new ApiException(404, "Role not found.");
        }
    }

    private static List<int> ValidateSelection(List<int>? ids)
    {
        if (ids == null || ids.Count > 100 || new HashSet<int>(ids).Count != ids.Count)
        {
            throw new ApiException(400, "Supply up to 100 distinct IDs; use an empty list to remove all assignments.");
        }
        return ids;
    }

    private static SystemFeatureResponse MapFeature(SystemFeature feature)
    {
        return new SystemFeatureResponse { Id = feature.Id, Code = feature.Code, Description = feature.Description };
    }
}
