using Lalamog.Api.Data;
using Lalamog.Api.Authorization;
using Lalamog.Api.Infrastructure;
using Lalamog.Api.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
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
        Assert.Equal(4, await db.Roles.CountAsync());
        Assert.Equal(13, await db.SystemFeatures.CountAsync());
        Assert.Equal(20, await db.RoleSystemFeatures.CountAsync());
        Assert.Equal(23, await db.UserRoles.CountAsync());
        Assert.Equal(22, await db.Users.CountAsync());
        Assert.Equal(22, await db.Profiles.CountAsync());
        Assert.Equal(11, await db.Swipes.CountAsync());
        Assert.Equal(5, await db.Matches.CountAsync());
        Assert.Equal(10, await db.Messages.CountAsync());
        Assert.Equal(10, await db.Fights.CountAsync());
        Assert.Equal(17, await db.FightConfirmations.CountAsync());

        var users = await db.Users.Include(u => u.Profile).ToListAsync();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();
        foreach (var user in users.Where(u => u.Email != "owner.seed@example.test"))
        {
            Assert.True(PasswordHashing.Verify(user.PasswordHash, "Lalamog123!Test"));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.FighterName));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Bio));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Location));
            Assert.False(string.IsNullOrWhiteSpace(user.Profile.Availability));
            var photo = await profileService.OpenAvatarAsync(user.Id, CancellationToken.None);
            await using (photo.Content)
            {
                using var decoded = SKBitmap.Decode(photo.Content);
                Assert.NotNull(decoded);
                Assert.True(decoded.Width > 0 && decoded.Height > 0);
            }
        }
        var fights = await db.Fights.Include(f => f.Match).Include(f => f.Confirmations).ToListAsync();
        Assert.Equal(1, fights.Count(f => f.Status == "scheduled"));
        Assert.Equal(3, fights.Count(f => f.Status == "confirmed"));
        Assert.Equal(5, fights.Count(f => f.Status == "completed"));
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

        string[] discoveryEmails =
        {
            "nico.seed@example.test", "bea.seed@example.test", "carlo.seed@example.test", "dani.seed@example.test",
            "enzo.seed@example.test", "faye.seed@example.test", "gio.seed@example.test", "hana.seed@example.test",
            "ivan.seed@example.test", "luna.seed@example.test", "omar.seed@example.test", "pia.seed@example.test",
            "rafa.seed@example.test", "tala.seed@example.test", "yuri.seed@example.test"
        };
        var discoveryUsers = (from user in users where discoveryEmails.Contains(user.Email) select user).ToList();
        Assert.Equal(15, discoveryUsers.Count);
        foreach (var candidate in discoveryUsers)
        {
            Assert.False(await (from swipe in db.Swipes
                                where swipe.SwiperId == candidate.Id || swipe.TargetId == candidate.Id
                                select swipe).AnyAsync());
            Assert.False(await (from match in db.Matches
                                where match.User1Id == candidate.Id || match.User2Id == candidate.Id
                                select match).AnyAsync());
        }
        foreach (var viewer in users)
        {
            var discovery = await profileService.DiscoverAsync(viewer.Id, CancellationToken.None);
            foreach (var candidate in discoveryUsers)
            {
                if (candidate.Id != viewer.Id)
                {
                    Assert.Contains(candidate.Id, from profile in discovery select profile.UserId);
                }
            }
        }

        var editedGym = await (from gym in db.Gyms where gym.Slug == "demo-antipolo-gym-1" select gym).SingleAsync();
        editedGym.Name = "Keep gym edit"; editedGym.SessionPrice = 777; editedGym.IsPublished = false;
        var ownerGrant = await (from grant in db.RoleSystemFeatures where grant.Role.Name == "GYM_OWNER" && grant.SystemFeature.Code == ApplicationPermissions.Owner_ManageBookings select grant).SingleAsync();
        db.RoleSystemFeatures.Remove(ownerGrant);
        alex.Profile.Bio = "Edited after seeding; keep this.";
        var canceled = fights.Single(f => f.Location == "Seed QC weekend court");
        canceled.Status = "canceled";
        canceled.CancellationReason = "Changed during a test.";
        var adminRole = await (from role in db.Roles where role.Name == "ADMIN" select role).SingleAsync();
        var uploadGrant = await (from grant in db.RoleSystemFeatures where grant.Role.Name == "FIGHTER" && grant.SystemFeature.Code == ApplicationPermissions.User_Upload select grant).SingleAsync();
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
        Assert.Equal(22, await db.Users.CountAsync());
        Assert.Equal(22, await db.Profiles.CountAsync());
        Assert.Equal(11, await db.Swipes.CountAsync());
        Assert.Equal(5, await db.Matches.CountAsync());
        Assert.Equal(10, await db.Messages.CountAsync());
        Assert.Equal(17, await db.FightConfirmations.CountAsync());
        Assert.Equal(originalFightIds, await db.Fights.OrderBy(f => f.Id).Select(f => f.Id).ToArrayAsync());
        Assert.Equal(originalHash, (await db.Users.FindAsync(alex.Id))!.PasswordHash);
        Assert.Equal("Edited after seeding; keep this.", (await db.Profiles.FindAsync(alex.Id))!.Bio);
        Assert.Equal("canceled", (await db.Fights.FindAsync(canceled.Id))!.Status);
        Assert.False(await (from grant in db.RoleSystemFeatures where grant.Role.Name == "FIGHTER" && grant.SystemFeatureId == uploadId select grant).AnyAsync());
        Assert.False(await (from assignment in db.UserRoles where assignment.UserId == alex.Id && assignment.RoleId == adminId select assignment).AnyAsync());
        Assert.Equal("Keep this role edit.", (await db.Roles.FindAsync(adminId))!.Description);
        Assert.Equal(13, await db.SystemFeatures.CountAsync());
        Assert.Equal(4, await db.Roles.CountAsync());
        var preservedGym = (await db.Gyms.FindAsync(editedGym.Id))!;
        Assert.Equal("Keep gym edit", preservedGym.Name); Assert.Equal(777, preservedGym.SessionPrice); Assert.False(preservedGym.IsPublished);
        Assert.False(await (from grant in db.RoleSystemFeatures where grant.Role.Name == "GYM_OWNER" && grant.SystemFeature.Code == ApplicationPermissions.Owner_ManageBookings select grant).AnyAsync());
    }
}
