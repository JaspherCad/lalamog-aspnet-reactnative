using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace MyApi.Models
{
    public class Profile
    {
        [Key]
        public Guid Id { get; set; }

        // One-to-one 
        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [StringLength(50)]
        public string? Nickname { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        // Geography Point (WGS84)
        public Point? Location { get; set; }

        // #warning Point requires X and Y as input in DTO!
        // profile.Location = geometryFactory.CreatePoint(new Coordinate(
        //             updateDto.Location.X, // longitude
        //             updateDto.Location.Y  // latitude
        //         )); WHERE LOCATION DTO IS double x and double y 
        // #end-warning



        public string? FightingStyle { get; set; } // "Boxer", "MMA"

        // Experience Level: 1 = Beginner, 2 = Intermediate, 3 = Expert
        public int? ExperienceLevel { get; set; }

        // Navigation property for availability (One-to-One relationship)
        public Availability? Availability { get; set; }

        public string? ProfilePictureUrl { get; set; }














        // Competitive Statistics (updated when fight results are recorded)
        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;
        public int TotalDraws { get; set; } = 0;
        public int TotalFights { get; set; } = 0;
        
        // Win percentage (calculated property)
        public double WinPercentage => TotalFights > 0 ? (double)TotalWins / TotalFights * 100 : 0;
        
        // Elo-style rating system
        public int Rating { get; set; } = 1200; // Starting rating
        
        // Current win/loss streak
        public int CurrentWinStreak { get; set; } = 0;
        public int CurrentLossStreak { get; set; } = 0;
        public int BestWinStreak { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }















    // Relational table for availability instead of JSONB
    public class Availability //DEPRECATED. bro gumawa ng table for this. bad move tho
    {
        [Key]
        public Guid UserId { get; set; } // Primary key and foreign key

        [ForeignKey("UserId")]
        public Profile? Profile { get; set; }

        // PostgreSQL TEXT[] for array of days
        public string[]? Days { get; set; } // e.g., ["Mon", "Wed", "Fri"]

        [StringLength(50)]
        public string? Time { get; set; }   // e.g., "18:00-22:00"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
