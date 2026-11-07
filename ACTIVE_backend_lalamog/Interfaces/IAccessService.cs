using Lalamog.Api.DTOs;

namespace Lalamog.Api.Interfaces;

public interface IAccessService
{
    Task<Lalamog.Api.DTOs.AuthResponse> SessionAsync(Guid userId, CancellationToken cancellationToken);
    Task EnableFighterAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<Lalamog.Api.DTOs.AccessUserResponse>> ListUsersAsync(CancellationToken cancellationToken);
    Task<bool> HasVisitorFeatureAsync(string code, CancellationToken cancellationToken);
    Task<bool> HasFeatureAsync(Guid userId, string code, CancellationToken cancellationToken);
    Task<List<SystemFeatureResponse>> ListFeaturesAsync(CancellationToken cancellationToken);
    Task<List<RoleResponse>> ListRolesAsync(CancellationToken cancellationToken);
    Task<UserRolesResponse> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken);
    Task SetRoleFeaturesAsync(int roleId, SetRoleSystemFeaturesRequest request, CancellationToken cancellationToken);
}
