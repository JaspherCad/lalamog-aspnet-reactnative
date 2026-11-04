using Lalamog.Api.Data;
using Lalamog.Api.Authorization;
using Lalamog.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Lalamog.Api.Tests;

public class DatabaseSeederTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _app;

    public DatabaseSeederTests(ApiFixture app)
    {
        _app = app;
    }

    [Fact]
    public async Task StaticSeederFillsEveryModelAndCanRepeatWithoutResettingEdits()
    {
        using IServiceScope scope = _app.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(8, await db.SystemFeatures.CountAsync());
        Assert.Equal(8, await db.RoleSystemFeatures.CountAsync());
        Assert.Equal(7, await db.UserRoles.CountAsync());
        Assert.Equal(6, await db.Users.CountAsync());
        Assert.Equal(6, await db.Profiles.CountAsync());
        Assert.Equal(11, await db.Swipes.CountAsync());
        Assert.Equal(5, await db.Matches.CountAsync());
        Assert.Equal(10, await db.Messages.CountAsync());
        Assert.Equal(6, await db.Fights.CountAsync());
        Assert.Equal(9, await db.FightConfirmations.CountAsync());

        var users = await db.Users.Include(u => u.Profile).ToListAsync();
        foreach (var user in users)
        {
            Assert.True(PasswordHashing.Verify(user.PasswordHash, "Lalamog123!Test"));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.FighterName));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Bio));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Location));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Availability));
        }
        var fights = await db.Fights.Include(f => f.Match).Include(f => f.Confirmations).ToListAsync();
        Assert.Equal(1, fights.Count(f => f.Status == "scheduled"));
        Assert.Equal(2, fights.Count(f => f.Status == "confirmed"));
        Assert.Equal(2, fights.Count(f => f.Status == "completed"));
        Assert.Equal(1, fights.Count(f => f.Status == "canceled"));
        foreach (var fight in fights)
        {
            Assert.True(string.CompareOrdinal(fight.Match.User1Id.ToString("D"), fight.Match.User2Id.ToString("D")) < 0);
            foreach (var confirmation in fight.Confirmations)
            {
                Assert.True(fight.Match.HasMember(confirmation.UserId));
                Assert.False(string.IsNullOrWhiteSpace(confirmation.EmergencyContactName));
                Assert.False(string.IsNullOrWhiteSpace(confirmation.EmergencyContactPhone));
            }
        }
        var alex = users.Single(u => u.Email == "alex.seed@example.test");
        var ana = users.Single(u => u.Email == "ana.seed@example.test");
        Assert.False(await db.Matches.AnyAsync(m => m.User1Id == ana.Id || m.User2Id == ana.Id));
        Assert.False(await db.Swipes.AnyAsync(s => s.SwiperId == alex.Id && s.TargetId == ana.Id));

        alex.Profile.Bio = "Edited after seeding; keep this.";
        var canceled = fights.Single(f => f.Location == "Seed QC weekend court");
        canceled.Status = "canceled";
        canceled.CancellationReason = "Changed during a test.";
        var adminRole = await (from role in db.Roles where role.Name == "Admin" select role).SingleAsync();
        var uploadGrant = await (from grant in db.RoleSystemFeatures where grant.SystemFeature.Code == ApplicationPermissions.User_Upload select grant).SingleAsync();
        db.RoleSystemFeatures.Remove(uploadGrant);
        db.UserRoles.Remove(await (from assignment in db.UserRoles where assignment.UserId == alex.Id && assignment.RoleId == adminRole.Id select assignment).SingleAsync());
        adminRole.Description = "Keep this role edit.";
        await db.SaveChangesAsync();
        int uploadId = uploadGrant.SystemFeatureId;
        int adminId = adminRole.Id;
        string? originalHash = alex.PasswordHash;
        long[] originalFightIds = fights.Select(f => f.Id).Order().ToArray();
        db.ChangeTracker.Clear();

        await DatabaseSeeder.SeedAsync(db);
        await DatabaseSeeder.SeedAsync(db);
        Assert.Equal(6, await db.Users.CountAsync());
        Assert.Equal(6, await db.Profiles.CountAsync());
        Assert.Equal(11, await db.Swipes.CountAsync());
        Assert.Equal(5, await db.Matches.CountAsync());
        Assert.Equal(10, await db.Messages.CountAsync());
        Assert.Equal(9, await db.FightConfirmations.CountAsync());
        Assert.Equal(originalFightIds, await db.Fights.OrderBy(f => f.Id).Select(f => f.Id).ToArrayAsync());
        Assert.Equal(originalHash, (await db.Users.FindAsync(alex.Id))!.PasswordHash);
        Assert.Equal("Edited after seeding; keep this.", (await db.Profiles.FindAsync(alex.Id))!.Bio);
        Assert.Equal("canceled", (await db.Fights.FindAsync(canceled.Id))!.Status);
        Assert.False(await (from grant in db.RoleSystemFeatures where grant.SystemFeatureId == uploadId select grant).AnyAsync());
        Assert.False(await (from assignment in db.UserRoles where assignment.UserId == alex.Id && assignment.RoleId == adminId select assignment).AnyAsync());
        Assert.Equal("Keep this role edit.", (await db.Roles.FindAsync(adminId))!.Description);
        Assert.Equal(8, await db.SystemFeatures.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
    }
}
