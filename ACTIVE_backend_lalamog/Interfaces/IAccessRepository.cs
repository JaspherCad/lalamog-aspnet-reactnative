using Lalamog.Api.Models;

namespace Lalamog.Api.Interfaces;

public interface IAccessRepository
{
    Task<bool> HasFeatureAsync(Guid userId, string code, CancellationToken cancellationToken);
    Task<Role?> FindRoleAsync(string name, CancellationToken cancellationToken);
    Task<List<SystemFeature>> ListFeaturesAsync(CancellationToken cancellationToken);
    Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken);
    Task<List<int>?> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> RoleIdsExistAsync(List<int> roleIds, CancellationToken cancellationToken);
    Task<bool> FeatureIdsExistAsync(List<int> featureIds, CancellationToken cancellationToken);
    Task<bool> SetUserRolesAsync(Guid userId, List<int> roleIds, CancellationToken cancellationToken);
    Task<bool> SetRoleFeaturesAsync(int roleId, List<int> featureIds, CancellationToken cancellationToken);
}
