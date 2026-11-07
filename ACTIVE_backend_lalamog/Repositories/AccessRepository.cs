using Lalamog.Api.Data;
using Lalamog.Api.Authorization;
using Lalamog.Api.DTOs;
using Lalamog.Api.Infrastructure;
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

    public async Task<AuthResponse> SessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var roles = from link in _db.UserRoles where link.UserId == userId orderby link.Role.Name select link.Role.Name;
        var permissions = from link in _db.UserRoles
                          join grant in _db.RoleSystemFeatures on link.RoleId equals grant.RoleId
                          where link.UserId == userId select grant.SystemFeature.Code;
        return new AuthResponse { UserId = userId, Roles = await roles.ToListAsync(cancellationToken),
            Permissions = await permissions.Distinct().ToListAsync(cancellationToken) };
    }
    public async Task EnableFighterAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await HasFeatureAsync(userId, ApplicationPermissions.Owner_EnableFighter, cancellationToken))
        { throw new ApiException(403, "Fighter opt-in permission is required."); }
        var role = await (from item in _db.Roles where item.Name == "FIGHTER" select item).SingleAsync(cancellationToken);
        if (!await (from link in _db.UserRoles where link.UserId == userId && link.RoleId == role.Id select link).AnyAsync(cancellationToken))
        {
            _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
    public async Task<List<AccessUserResponse>> ListUsersAsync(CancellationToken cancellationToken)
    {
        var users = await (from user in _db.Users.Include(nameof(AppUser.Profile)).Include(nameof(AppUser.UserRoles))
                           orderby user.Id select user).Take(100).ToListAsync(cancellationToken);
        var eligible = await (from link in _db.UserRoles
                              join grant in _db.RoleSystemFeatures on link.RoleId equals grant.RoleId
                              where grant.SystemFeature.Code == ApplicationPermissions.Owner_ManageGyms
                              select link.UserId).Distinct().ToListAsync(cancellationToken);
        var response = new List<AccessUserResponse>();
        foreach (var user in users)
        {
            response.Add(new AccessUserResponse { UserId = user.Id, DisplayName = user.Profile.Nickname, CanManageGyms = eligible.Contains(user.Id),
                RoleIds = (from link in user.UserRoles select link.RoleId).ToList() });
        }
        return response;
    }

    public Task<bool> HasVisitorFeatureAsync(string code, CancellationToken cancellationToken)
    {
        // Only deliberately public permissions can be evaluated without an account.
        if (code != ApplicationPermissions.Visitor_ViewGyms) { return Task.FromResult(false); }
        var query = from grant in _db.RoleSystemFeatures
                    where grant.Role.Name == "VISITOR" && grant.SystemFeature.Code == code select grant;
        return query.AnyAsync(cancellationToken);
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
        var query = from role in _db.Roles where roleIds.Contains(role.Id) && role.Name != "VISITOR" select role.Id;
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
