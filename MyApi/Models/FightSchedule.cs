using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MyApi.Models
{
    public class FightSchedule
    {
        [Key]
        public long Id { get; set; }

        //#note match details -> linked to match (Many FS to One Match)
        [Required]
        public long MatchId { get; set; }
        public Match Match { get; set; } = null!;


        [Required]
        public DateTime ScheduledDateTime { get; set; }



        // Status constraint: 'scheduled', 'confirmed', 'in-progress', 'completed', 'canceled'
        [Required]
        [RegularExpression("scheduled|confirmed|in-progress|completed|canceled")]
        public string Status { get; set; } = "scheduled";




        [Required]
        public string LocationName { get; set; } = string.Empty; // Gym name or location

        [Required]
        public string LocationAddress { get; set; } = string.Empty;
        //#note: Location Details
        // #info Point requires X and Y as input in DTO!
        // sample service snippet
        // profile.Location = geometryFactory.CreatePoint(new Coordinate(
        //             updateDto.Location.X, // longitude
        //             updateDto.Location.Y  // latitude
        //         )); WHERE LOCATION DTO IS double x and double y 
        // #end-info

        public Point LocationCoordinates { get; set; } = null!;


        // #info this serves as the "ACCEPT" button for both users
        public bool IsSafetyWaiverAcceptedByUser1 { get; set; }
        public bool IsSafetyWaiverAcceptedByUser2 { get; set; }

        // Skill level confirmation (to prevent dangerous mismatches)
        [Required]
        public int User1SkillLevelAtTimeOfScheduling { get; set; }

        [Required]
        public int User2SkillLevelAtTimeOfScheduling { get; set; }

        // Emergency contact info (per user)
        [Required]
        public string User1EmergencyContactName { get; set; } = string.Empty;

        [Required]
        public string User1EmergencyContactPhone { get; set; } = string.Empty;

        [Required]
        public string User2EmergencyContactName { get; set; } = string.Empty;

        [Required]
        public string User2EmergencyContactPhone { get; set; } = string.Empty;

        // Cancellation details
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? CanceledAt { get; set; }
        public Guid? CanceledByUserId { get; set; }

        // Created and updated timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // #warning: OPTIONAL Post-fight rating
        public int? User1Rating { get; set; }
        public int? User2Rating { get; set; }
        public string? User1Feedback { get; set; }
        public string? User2Feedback { get; set; }









        // #note
        // PSEUDOCODE
        // in a match, we have many fightSchedule where
        // FightSchedule1 → WinnerId = user1Id → User1 wins++, User2 losses++
        // FightSchedule2 → WinnerId = user1Id → User1 wins++, User2 losses++
        // FightSchedule3 → WinnerId = user2Id → User2 wins++, User1 losses++
        // #end-note


        // Fight Result Tracking (FOR THIS SPECIFIC FIGHT SCHEDULE)
        public Guid? WinnerId { get; set; } // null if draw/no result yet
        public string FightResult { get; set; } = "pending"; // "user1_win", "user2_win", "draw", "no_contest", "pending"
        public string? WinMethod { get; set; } // "KO", "TKO", "Decision", "Submission", etc.c
        public int? FightDurationMinutes { get; set; }
        public string? ResultNotes { get; set; }

        // Rating changes for this fight
        public int? User1RatingBefore { get; set; }
        public int? User1RatingAfter { get; set; }
        public int? User2RatingBefore { get; set; }
        public int? User2RatingAfter { get; set; }

        // When the result was recorded
        public DateTime? ResultRecordedAt { get; set; }
        public Guid? ResultRecordedByUserId { get; set; } // Who recorded the result


    }
}