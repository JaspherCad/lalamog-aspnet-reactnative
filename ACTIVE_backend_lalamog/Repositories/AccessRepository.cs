using Lalamog.Api.Data;
using Lalamog.Api.Interfaces;
using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Lalamog.Api.Repositories;

public class AccessRepository : IAccessRepository
{
    private readonly AppDbContext _db;

    public AccessRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<bool> HasFeatureAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var query = from userRole in _db.UserRoles
                    join roleFeature in _db.RoleSystemFeatures on userRole.RoleId equals roleFeature.RoleId
                    join feature in _db.SystemFeatures on roleFeature.SystemFeatureId equals feature.Id
                    where userRole.UserId == userId && feature.Code == code
                    select feature.Id;
        return query.AnyAsync(cancellationToken);
    }

    public Task<Role?> FindRoleAsync(string name, CancellationToken cancellationToken)
    {
        var query = from role in _db.Roles.AsNoTracking()
                    where role.Name == name
                    select role;
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    public Task<List<SystemFeature>> ListFeaturesAsync(CancellationToken cancellationToken)
    {
        var query = from feature in _db.SystemFeatures.AsNoTracking()
                    orderby feature.Code
                    select feature;
        return query.ToListAsync(cancellationToken);
    }

    public Task<List<Role>> ListRolesAsync(CancellationToken cancellationToken)
    {
        var query = from role in _db.Roles.AsNoTracking().Include("RoleSystemFeatures.SystemFeature")
                    orderby role.Name
                    select role;
        return query.ToListAsync(cancellationToken);
    }

    public async Task<List<int>?> GetUserRoleIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var users = from user in _db.Users where user.Id == userId select user.Id;
        if (!await users.AnyAsync(cancellationToken))
        {
            return null;
        }
        var roles = from userRole in _db.UserRoles
                    where userRole.UserId == userId
                    orderby userRole.RoleId
                    select userRole.RoleId;
        return await roles.ToListAsync(cancellationToken);
    }

    public async Task<bool> RoleIdsExistAsync(List<int> roleIds, CancellationToken cancellationToken)
    {
        var query = from role in _db.Roles where roleIds.Contains(role.Id) select role.Id;
        return await query.CountAsync(cancellationToken) == roleIds.Count;
    }

    public async Task<bool> FeatureIdsExistAsync(List<int> featureIds, CancellationToken cancellationToken)
    {
        var query = from feature in _db.SystemFeatures where featureIds.Contains(feature.Id) select feature.Id;
        return await query.CountAsync(cancellationToken) == featureIds.Count;
    }

    public async Task<bool> SetUserRolesAsync(Guid userId, List<int> roleIds, CancellationToken cancellationToken)
    {
        var query = from user in _db.Users.Include(nameof(AppUser.UserRoles))
                    where user.Id == userId select user;
        AppUser? account = await query.SingleOrDefaultAsync(cancellationToken);
        if (account == null)
        {
            return false;
        }
        var existing = new HashSet<int>();
        foreach (UserRole assignment in account.UserRoles.ToArray())
        {
            existing.Add(assignment.RoleId);
            if (!roleIds.Contains(assignment.RoleId))
            {
                _db.UserRoles.Remove(assignment);
            }
        }
        foreach (int roleId in roleIds)
        {
            if (!existing.Contains(roleId))
            {
                account.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetRoleFeaturesAsync(int roleId, List<int> featureIds, CancellationToken cancellationToken)
    {
        var query = from role in _db.Roles.Include(nameof(Role.RoleSystemFeatures))
                    where role.Id == roleId select role;
        Role? stored = await query.SingleOrDefaultAsync(cancellationToken);
        if (stored == null)
        {
            return false;
        }
        var existing = new HashSet<int>();
        foreach (RoleSystemFeature grant in stored.RoleSystemFeatures.ToArray())
        {
            existing.Add(grant.SystemFeatureId);
            if (!featureIds.Contains(grant.SystemFeatureId))
            {
                _db.RoleSystemFeatures.Remove(grant);
            }
        }
        foreach (int featureId in featureIds)
        {
            if (!existing.Contains(featureId))
            {
                stored.RoleSystemFeatures.Add(new RoleSystemFeature { RoleId = roleId, SystemFeatureId = featureId });
            }
        }
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
