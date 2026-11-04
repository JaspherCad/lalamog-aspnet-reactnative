using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces;

public interface IAccessService
{
    Task<bool> HasFeatureAsync(Guid userId, string code, CancellationToken cancellationToken);
    Task<List<SystemFeatureResponse>> ListFeaturesAsync(CancellationToken cancellationToken);
    Task<List<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken);
    Task<UserRolesResponse> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken);
    Task SetRoleFeaturesAsync(int roleId, SetRoleSystemFeaturesRequest request, CancellationToken cancellationToken);
}
