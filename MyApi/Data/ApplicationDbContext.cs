using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite;


using MyApi.Models;

namespace MyApi.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opts)
            : base(opts) { } //base is just the constructor
                             //constructor DI ito.

        public DbSet<WeatherForecast> Forecasts { get; set; }

        public DbSet<FightSchedule> FightSchedules { get; set; }



        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<SwipeAction> SwipeActions { get; set; }
        public DbSet<Availability> Availabilities { get; set; }

        public DbSet<Message> Messages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Call base to configure Identity entities
            base.OnModelCreating(modelBuilder); //base is just the constructor

            // Profile configuration
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.Property(p => p.Location)
                    .HasColumnType("geography"); // PostGIS geography type

                // One-to-one relationship with Availability
                entity.HasOne(p => p.Availability)
                    .WithOne(a => a.Profile)
                    .HasForeignKey<Availability>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Availability configuration
            modelBuilder.Entity<Availability>(entity =>
            {
                entity.HasKey(a => a.UserId); // UserId is the primary key

                entity.Property(a => a.Days)
                    .HasColumnType("text[]"); // PostgreSQL TEXT[] array type

                entity.Property(a => a.Time)
                    .HasMaxLength(50);
            });

            // Match configuration
            modelBuilder.Entity<Match>(entity =>
            {
                entity.HasIndex(match => new { match.User1Id, match.User2Id }).IsUnique();

                entity.HasCheckConstraint("CK_Match_Status",
                    "\"Status\" IN ('pending', 'active', 'ended')");

                entity.HasCheckConstraint("CK_Match_NoSelfMatch",
                    "\"User1Id\" != \"User2Id\"");


                // naging many to many relationship.. this becomes conjunction table.
                entity.HasOne(match => match.User1)
                    .WithMany()
                    .HasForeignKey(match => match.User1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(match => match.User2)
                    .WithMany()
                    .HasForeignKey(match => match.User2Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id); // Keep Id as long to match existing database


                // many message to one match
                entity.HasOne(m => m.Match)
                    .WithMany(m => m.ListOfMessages)
                    .HasForeignKey(m => m.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);


                // Many messages to one sender
                entity.HasOne(m => m.Sender)
                    .WithMany()
                    .HasForeignKey(m => m.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Many messages to one receiver
                entity.HasOne(m => m.Receiver)
                    .WithMany()
                    .HasForeignKey(m => m.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict);


                // add index for performance
                entity.HasIndex(m => new { m.SenderId, m.ReceiverId });

                entity.HasIndex(m => m.MatchId);



                // Properties configuration

                entity.Property(m => m.Content)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(m => m.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(m => m.Read)
                    .HasDefaultValue(false);
            });


            modelBuilder.Entity<FightSchedule>(entity =>
            {
                entity.HasKey(fs => fs.Id);

                entity.Property(fs => fs.LocationCoordinates)
                    .HasColumnType("geography"); // PostGIS geography type

                // Check constraint for status values
                entity.HasCheckConstraint("CK_FightSchedule_Status",
                    "\"Status\" IN ('scheduled', 'confirmed', 'in-progress', 'completed', 'canceled')");


                // Ensure scheduled time is in future
                entity.HasCheckConstraint("CK_FightSchedule_FutureTime",
                    "\"ScheduledDateTime\" > NOW()");

                // Skill level constraints (1-5 scale)
                entity.HasCheckConstraint("CK_FightSchedule_SkillLevel",
                    "\"User1SkillLevelAtTimeOfScheduling\" BETWEEN 1 AND 5 AND " +
                    "\"User2SkillLevelAtTimeOfScheduling\" BETWEEN 1 AND 5");


                // Relationship with Match 
                // #note: Many (FightSchedule) to One (Match)
                entity.HasOne(fs => fs.Match)
                    .WithMany(m => m.FightSchedules)
                    .HasForeignKey(fs => fs.MatchId)
                    .OnDelete(DeleteBehavior.Cascade);
            });



            // SwipeAction configuration
            modelBuilder.Entity<SwipeAction>(entity =>
            {
                entity.HasIndex(s => new
                {
                    s.SwiperId,
                    s.SwipeeId
                }).IsUnique();

                entity.HasCheckConstraint("CK_Swipe_Direction",
                                "\"Direction\" IN ('right', 'left')");

                //Many swipe actions to one swiper and swipee
                entity.HasOne(s => s.Swiper)
                    .WithMany()
                    .HasForeignKey(s => s.SwiperId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Swipee)
                                .WithMany()
                                .HasForeignKey(s => s.SwipeeId)
                                .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}