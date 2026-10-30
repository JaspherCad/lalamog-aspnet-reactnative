using Lalamog.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lalamog.Api.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
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

        protected override void OnModelCreating(ModelBuilder model)
        {
            base.OnModelCreating(model);

            var profile = model.Entity<FighterProfile>();
            profile.HasKey(nameof(FighterProfile.UserId));
            profile.HasOne<ApplicationUser>(nameof(FighterProfile.User))
                .WithOne(nameof(ApplicationUser.Profile))
                .HasForeignKey<FighterProfile>(nameof(FighterProfile.UserId));
            profile.Property(nameof(FighterProfile.Nickname)).HasMaxLength(50);
            profile.Property(nameof(FighterProfile.Bio)).HasMaxLength(500);
            profile.Property(nameof(FighterProfile.FightingStyle)).HasMaxLength(50);
            profile.Property(nameof(FighterProfile.Location)).HasMaxLength(200);
            profile.Property(nameof(FighterProfile.Availability)).HasMaxLength(200);
            profile.Property(nameof(FighterProfile.AvatarKey)).HasMaxLength(100);
            profile.ToTable(ConfigureProfileTable);

            var swipe = model.Entity<Swipe>();
            swipe.HasIndex(nameof(Swipe.SwiperId), nameof(Swipe.TargetId)).IsUnique();
            swipe.Property(nameof(Swipe.Direction)).HasMaxLength(8);
            swipe.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(Swipe.SwiperId))
                .OnDelete(DeleteBehavior.Restrict);
            swipe.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(Swipe.TargetId))
                .OnDelete(DeleteBehavior.Restrict);
            swipe.ToTable(ConfigureSwipeTable);

            var match = model.Entity<FighterMatch>();
            match.HasIndex(nameof(FighterMatch.User1Id), nameof(FighterMatch.User2Id)).IsUnique();
            match.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(FighterMatch.User1Id))
                .OnDelete(DeleteBehavior.Restrict);
            match.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(FighterMatch.User2Id))
                .OnDelete(DeleteBehavior.Restrict);
            match.ToTable(ConfigureMatchTable);

            var message = model.Entity<ChatMessage>();
            message.Property(nameof(ChatMessage.Content)).HasMaxLength(2000);
            message.HasIndex(nameof(ChatMessage.MatchId), nameof(ChatMessage.Id));
            message.HasOne<FighterMatch>().WithMany().HasForeignKey(nameof(ChatMessage.MatchId))
                .OnDelete(DeleteBehavior.Restrict);
            message.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(ChatMessage.SenderId))
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
            fight.ToTable(ConfigureFightTable);

            var confirmation = model.Entity<FightConfirmation>();
            confirmation.HasKey(nameof(FightConfirmation.FightId), nameof(FightConfirmation.UserId));
            confirmation.HasOne<Fight>(nameof(FightConfirmation.Fight))
                .WithMany(nameof(Fight.Confirmations)).HasForeignKey(nameof(FightConfirmation.FightId));
            confirmation.HasOne<ApplicationUser>().WithMany().HasForeignKey(nameof(FightConfirmation.UserId))
                .OnDelete(DeleteBehavior.Restrict);
            confirmation.Property(nameof(FightConfirmation.EmergencyContactName)).HasMaxLength(100);
            confirmation.Property(nameof(FightConfirmation.EmergencyContactPhone)).HasMaxLength(40);
        }

        private static void ConfigureProfileTable(TableBuilder<FighterProfile> table)
        {
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
            table.HasCheckConstraint("CK_Fight_Status", "[Status] IN ('scheduled', 'confirmed', 'canceled', 'completed')");
        }
    }
}
