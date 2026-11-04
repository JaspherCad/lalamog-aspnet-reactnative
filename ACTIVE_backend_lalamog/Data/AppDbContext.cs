using Lalamog.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lalamog.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users
        {
            get { return Set<AppUser>(); }
        }

        public DbSet<FighterProfile> Profiles
        {
            get { return Set<FighterProfile>(); }
        }

        public DbSet<Swipe> Swipes
        {
            get { return Set<Swipe>(); }
        }

        public DbSet<FighterMatch> Matches
        {
            get { return Set<FighterMatch>(); }
        }

        public DbSet<ChatMessage> Messages
        {
            get { return Set<ChatMessage>(); }
        }

        public DbSet<Fight> Fights
        {
            get { return Set<Fight>(); }
        }

        public DbSet<FightConfirmation> FightConfirmations
        {
            get { return Set<FightConfirmation>(); }
        }

        public DbSet<Role> Roles
        {
            get { return Set<Role>(); }
        }

        public DbSet<SystemFeature> SystemFeatures
        {
            get { return Set<SystemFeature>(); }
        }

        public DbSet<UserRole> UserRoles
        {
            get { return Set<UserRole>(); }
        }

        public DbSet<RoleSystemFeature> RoleSystemFeatures
        {
            get { return Set<RoleSystemFeature>(); }
        }

        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            var user = model.Entity<AppUser>();
            user.ToTable("AppUsers");
            user.HasKey(nameof(AppUser.Id));
            user.Property(nameof(AppUser.Email)).HasMaxLength(256);
            user.Property(nameof(AppUser.NormalizedEmail)).HasMaxLength(256);
            user.HasIndex(nameof(AppUser.NormalizedEmail)).IsUnique();
            user.Property(nameof(AppUser.Version)).IsRowVersion();

            var role = model.Entity<Role>();
            role.Property(nameof(Role.Name)).HasMaxLength(50);
            role.HasIndex(nameof(Role.Name)).IsUnique();
            role.Property(nameof(Role.Description)).HasMaxLength(200);

            var feature = model.Entity<SystemFeature>();
            feature.Property(nameof(SystemFeature.Code)).HasMaxLength(80);
            feature.HasIndex(nameof(SystemFeature.Code)).IsUnique();
            feature.Property(nameof(SystemFeature.Description)).HasMaxLength(200);

            var userRole = model.Entity<UserRole>();
            userRole.HasKey(nameof(UserRole.UserId), nameof(UserRole.RoleId));
            userRole.HasOne<AppUser>(nameof(UserRole.User))
                .WithMany(nameof(AppUser.UserRoles)).HasForeignKey(nameof(UserRole.UserId))
                .OnDelete(DeleteBehavior.Restrict);
            userRole.HasOne<Role>(nameof(UserRole.Role))
                .WithMany(nameof(Role.UserRoles)).HasForeignKey(nameof(UserRole.RoleId))
                .OnDelete(DeleteBehavior.Restrict);

            var roleFeature = model.Entity<RoleSystemFeature>();
            roleFeature.HasKey(nameof(RoleSystemFeature.RoleId), nameof(RoleSystemFeature.SystemFeatureId));
            roleFeature.HasOne<Role>(nameof(RoleSystemFeature.Role))
                .WithMany(nameof(Role.RoleSystemFeatures)).HasForeignKey(nameof(RoleSystemFeature.RoleId))
                .OnDelete(DeleteBehavior.Restrict);
            roleFeature.HasOne<SystemFeature>(nameof(RoleSystemFeature.SystemFeature))
                .WithMany(nameof(SystemFeature.RoleSystemFeatures)).HasForeignKey(nameof(RoleSystemFeature.SystemFeatureId))
                .OnDelete(DeleteBehavior.Restrict);

            var profile = model.Entity<FighterProfile>();
            profile.HasKey(nameof(FighterProfile.UserId));
            profile.HasOne<AppUser>(nameof(FighterProfile.User))
                .WithOne(nameof(AppUser.Profile))
                .HasForeignKey<FighterProfile>(nameof(FighterProfile.UserId));
            profile.Property(nameof(FighterProfile.Nickname)).HasMaxLength(50);
            profile.Property(nameof(FighterProfile.FighterName)).HasMaxLength(50);
            profile.Property(nameof(FighterProfile.NormalizedFighterName)).HasMaxLength(50);
            profile.HasIndex(nameof(FighterProfile.NormalizedFighterName)).IsUnique();
            profile.Property(nameof(FighterProfile.Bio)).HasMaxLength(500);
            profile.Property(nameof(FighterProfile.FightingStyle)).HasMaxLength(50);
            profile.Property(nameof(FighterProfile.Location)).HasMaxLength(200);
            profile.Property(nameof(FighterProfile.Availability)).HasMaxLength(200);
            profile.Property(nameof(FighterProfile.AvatarKey)).HasMaxLength(100);
            profile.Property(nameof(FighterProfile.RegionCode)).HasMaxLength(10);
            profile.Property(nameof(FighterProfile.ProvinceCode)).HasMaxLength(10);
            profile.Property(nameof(FighterProfile.CityCode)).HasMaxLength(10);
            profile.Property(nameof(FighterProfile.BarangayCode)).HasMaxLength(10);
            profile.Property(nameof(FighterProfile.PsgcVersion)).HasMaxLength(16);
            profile.ToTable(ConfigureProfileTable);

            var swipe = model.Entity<Swipe>();
            swipe.HasIndex(nameof(Swipe.SwiperId), nameof(Swipe.TargetId)).IsUnique();
            swipe.Property(nameof(Swipe.Direction)).HasMaxLength(8);
            swipe.HasOne<AppUser>().WithMany().HasForeignKey(nameof(Swipe.SwiperId))
                .OnDelete(DeleteBehavior.Restrict);
            swipe.HasOne<AppUser>().WithMany().HasForeignKey(nameof(Swipe.TargetId))
                .OnDelete(DeleteBehavior.Restrict);
            swipe.ToTable(ConfigureSwipeTable);

            var match = model.Entity<FighterMatch>();
            match.HasIndex(nameof(FighterMatch.User1Id), nameof(FighterMatch.User2Id)).IsUnique();
            match.HasOne<AppUser>().WithMany().HasForeignKey(nameof(FighterMatch.User1Id))
                .OnDelete(DeleteBehavior.Restrict);
            match.HasOne<AppUser>().WithMany().HasForeignKey(nameof(FighterMatch.User2Id))
                .OnDelete(DeleteBehavior.Restrict);
            match.ToTable(ConfigureMatchTable);

            var message = model.Entity<ChatMessage>();
            message.Property(nameof(ChatMessage.Content)).HasMaxLength(2000);
            message.HasIndex(nameof(ChatMessage.MatchId), nameof(ChatMessage.Id));
            message.HasOne<FighterMatch>().WithMany().HasForeignKey(nameof(ChatMessage.MatchId))
                .OnDelete(DeleteBehavior.Restrict);
            message.HasOne<AppUser>().WithMany().HasForeignKey(nameof(ChatMessage.SenderId))
                .OnDelete(DeleteBehavior.Restrict);

            var fight = model.Entity<Fight>();
            fight.HasOne<FighterMatch>(nameof(Fight.Match)).WithMany().HasForeignKey(nameof(Fight.MatchId))
                .OnDelete(DeleteBehavior.Restrict);
            fight.HasIndex(nameof(Fight.MatchId)).IsUnique().HasFilter("[Status] IN ('scheduled', 'confirmed')");
            fight.Property(nameof(Fight.Location)).HasMaxLength(200);
            fight.Property(nameof(Fight.Address)).HasMaxLength(300);
            fight.Property(nameof(Fight.Status)).HasMaxLength(20);
            fight.Property(nameof(Fight.CancellationReason)).HasMaxLength(500);
            fight.Property(nameof(Fight.Version)).IsRowVersion();
            fight.Property(nameof(Fight.RegionCode)).HasMaxLength(10);
            fight.Property(nameof(Fight.ProvinceCode)).HasMaxLength(10);
            fight.Property(nameof(Fight.CityCode)).HasMaxLength(10);
            fight.Property(nameof(Fight.BarangayCode)).HasMaxLength(10);
            fight.Property(nameof(Fight.PsgcVersion)).HasMaxLength(16);
            fight.ToTable(ConfigureFightTable);

            var confirmation = model.Entity<FightConfirmation>();
            confirmation.HasKey(nameof(FightConfirmation.FightId), nameof(FightConfirmation.UserId));
            confirmation.HasOne<Fight>(nameof(FightConfirmation.Fight))
                .WithMany(nameof(Fight.Confirmations)).HasForeignKey(nameof(FightConfirmation.FightId));
            confirmation.HasOne<AppUser>().WithMany().HasForeignKey(nameof(FightConfirmation.UserId))
                .OnDelete(DeleteBehavior.Restrict);
            confirmation.Property(nameof(FightConfirmation.EmergencyContactName)).HasMaxLength(100);
            confirmation.Property(nameof(FightConfirmation.EmergencyContactPhone)).HasMaxLength(40);
        }

        private static void ConfigureProfileTable(TableBuilder<FighterProfile> table)
        {
            table.HasCheckConstraint("CK_Profile_Coordinates", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
            table.HasCheckConstraint("CK_Profile_Experience", "[ExperienceLevel] BETWEEN 1 AND 3");
        }

        private static void ConfigureSwipeTable(TableBuilder<Swipe> table)
        {
            table.HasCheckConstraint("CK_Swipe_Direction", "[Direction] IN ('like', 'pass')");
            table.HasCheckConstraint("CK_Swipe_NoSelf", "[SwiperId] <> [TargetId]");
        }

        private static void ConfigureMatchTable(TableBuilder<FighterMatch> table)
        {
            table.HasCheckConstraint("CK_Match_NoSelf", "[User1Id] <> [User2Id]");
        }

        private static void ConfigureFightTable(TableBuilder<Fight> table)
        {
            table.HasCheckConstraint("CK_Fight_Coordinates", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
            table.HasCheckConstraint("CK_Fight_Status", "[Status] IN ('scheduled', 'confirmed', 'canceled', 'completed')");
        }
    }
}
