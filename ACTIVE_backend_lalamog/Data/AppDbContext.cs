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

        public DbSet<Gym> Gyms { get { return Set<Gym>(); } }
        public DbSet<MapRegion> Regions { get { return Set<MapRegion>(); } }
public DbSet<MapProvince> Provinces { get { return Set<MapProvince>(); } }
public DbSet<MapCity> Cities { get { return Set<MapCity>(); } }
public DbSet<MapBarangay> Barangays { get { return Set<MapBarangay>(); } }

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
            var region = model.Entity<MapRegion>();
            region.Property(nameof(MapRegion.Code)).HasMaxLength(10);
            region.HasIndex(nameof(MapRegion.Code)).IsUnique();
            region.Property(nameof(MapRegion.Name)).HasMaxLength(200);
            var province = model.Entity<MapProvince>();
            province.Property(nameof(MapProvince.Code)).HasMaxLength(10);
            province.HasIndex(nameof(MapProvince.Code)).IsUnique();
            province.Property(nameof(MapProvince.Name)).HasMaxLength(200);
            var city = model.Entity<MapCity>();
            city.Property(nameof(MapCity.Code)).HasMaxLength(10);
            city.HasIndex(nameof(MapCity.Code)).IsUnique();
            city.Property(nameof(MapCity.Name)).HasMaxLength(200);
            var barangay = model.Entity<MapBarangay>();
            barangay.Property(nameof(MapBarangay.Code)).HasMaxLength(10);
            barangay.HasIndex(nameof(MapBarangay.Code)).IsUnique();
            barangay.Property(nameof(MapBarangay.Name)).HasMaxLength(200);
            province.HasOne<MapRegion>(nameof(MapProvince.Region)).WithMany(nameof(MapRegion.Provinces)).HasForeignKey(nameof(MapProvince.RegionId)).OnDelete(DeleteBehavior.Restrict);
            city.HasOne<MapProvince>(nameof(MapCity.Province)).WithMany(nameof(MapProvince.Cities)).HasForeignKey(nameof(MapCity.ProvinceId)).OnDelete(DeleteBehavior.Restrict);
            city.HasOne<MapRegion>(nameof(MapCity.Region)).WithMany(nameof(MapRegion.IndependentCities)).HasForeignKey(nameof(MapCity.RegionId)).OnDelete(DeleteBehavior.Restrict);
            city.ToTable(ConfigureCityTable);
            barangay.HasAlternateKey(nameof(MapBarangay.Id), nameof(MapBarangay.CityId));
            barangay.HasOne<MapCity>(nameof(MapBarangay.City)).WithMany(nameof(MapCity.Barangays)).HasForeignKey(nameof(MapBarangay.CityId)).OnDelete(DeleteBehavior.Restrict);
            var map = model.Entity<Gym>();
            map.Property(nameof(Gym.Slug)).HasMaxLength(80);
            map.HasIndex(nameof(Gym.Slug)).IsUnique();
            map.Property(nameof(Gym.Name)).HasMaxLength(200);
            map.Property(nameof(Gym.Address)).HasMaxLength(300);
            map.HasOne<MapCity>(nameof(Gym.City)).WithMany(nameof(MapCity.Gyms)).HasForeignKey(nameof(Gym.CityId)).OnDelete(DeleteBehavior.Restrict);
            map.HasOne<MapBarangay>(nameof(Gym.Barangay)).WithMany(nameof(MapBarangay.Gyms)).HasForeignKey(nameof(Gym.BarangayId), nameof(Gym.CityId)).HasPrincipalKey(nameof(MapBarangay.Id), nameof(MapBarangay.CityId)).OnDelete(DeleteBehavior.Restrict);
            map.HasOne<AppUser>(nameof(Gym.Owner)).WithMany().HasForeignKey(nameof(Gym.OwnerUserId)).OnDelete(DeleteBehavior.Restrict);
            map.Property(nameof(Gym.Description)).HasMaxLength(2000);
            map.Property(nameof(Gym.ContactInformation)).HasMaxLength(300);
            map.Property(nameof(Gym.SessionPrice)).HasPrecision(10, 2);
            map.Property(nameof(Gym.Version)).IsRowVersion();
            fight.Property(nameof(Fight.QuotedPrice)).HasPrecision(10, 2);
            fight.HasIndex(nameof(Fight.GymId), nameof(Fight.ScheduledAt));
            map.ToTable(ConfigureMapTable);
            fight.HasOne<Gym>(nameof(Fight.Gym)).WithMany(nameof(Gym.Fights)).HasForeignKey(nameof(Fight.GymId))
                .OnDelete(DeleteBehavior.Restrict);
            fight.HasOne<FighterMatch>(nameof(Fight.Match)).WithMany().HasForeignKey(nameof(Fight.MatchId))
                .OnDelete(DeleteBehavior.Restrict);
            fight.HasIndex(nameof(Fight.MatchId)).IsUnique().HasFilter("[Status] IN ('pending_approval', 'scheduled', 'confirmed')");
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
            table.HasCheckConstraint("CK_Fight_SharedVenue", "[GymId] IS NULL OR ([Location] = '' AND [Address] = '' AND [RegionCode] IS NULL AND [ProvinceCode] IS NULL AND [CityCode] IS NULL AND [BarangayCode] IS NULL AND [PsgcVersion] IS NULL AND [Latitude] IS NULL AND [Longitude] IS NULL)");
            table.HasCheckConstraint("CK_Fight_Coordinates", "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL AND [Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180)");
            table.HasCheckConstraint("CK_Fight_Status", "[Status] IN ('pending_approval', 'scheduled', 'confirmed', 'canceled', 'completed')");
        }

        private static void ConfigureCityTable(TableBuilder<MapCity> table)
        {
            table.HasCheckConstraint("CK_City_Parent", "([ProvinceId] IS NOT NULL AND [RegionId] IS NULL) OR ([ProvinceId] IS NULL AND [RegionId] IS NOT NULL)");
        }

        private static void ConfigureMapTable(TableBuilder<Gym> table)
        {
            table.HasCheckConstraint("CK_Gym_Details", "[SessionPrice] >= 0 AND [SessionDurationMinutes] BETWEEN 30 AND 240 AND ([IsPublished] = 0 OR [OwnerUserId] IS NOT NULL)");
            table.HasCheckConstraint("CK_Map_Coordinates", "[Latitude] BETWEEN -90 AND 90 AND [Longitude] BETWEEN -180 AND 180");
        }
    }
}
