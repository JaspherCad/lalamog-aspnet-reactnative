using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyApi.DTOs
{
    public class FightScheduleDto
    {
        public long Id { get; set; }
        public long MatchId { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string Status { get; set; } = "scheduled";
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;


        //#info existing DTO. (x,y)
        public LocationDto? LocationCoordinates { get; set; }

        public bool IsSafetyWaiverAcceptedByUser1 { get; set; }
        public bool IsSafetyWaiverAcceptedByUser2 { get; set; }
        public int User1SkillLevelAtTimeOfScheduling { get; set; }
        public int User2SkillLevelAtTimeOfScheduling { get; set; }
        public string User1EmergencyContactName { get; set; } = string.Empty;
        public string User1EmergencyContactPhone { get; set; } = string.Empty;
        public string User2EmergencyContactName { get; set; } = string.Empty;
        public string User2EmergencyContactPhone { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
        public DateTime? CanceledAt { get; set; }
        public Guid? CanceledByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? User1Rating { get; set; }
        public int? User2Rating { get; set; }
        public string? User1Feedback { get; set; }
        public string? User2Feedback { get; set; }
    }

    public class SafetyWaiverDto
    {
        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        // Add signature if you implement that
    }

    public class ScheduleFightRequestDto
    {
        // UserId removed - extracted from JWT token automatically
        public long MatchId { get; set; }
        public DateTime ScheduledDateTime { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        public LocationDto? LocationCoordinates { get; set; }
    }


    public class ConfirmFightRequestDto
    {
        // UserId removed - extracted from JWT token automatically
        public long FightScheduleId { get; set; }
        public SafetyWaiverDto? SafetyWaiver { get; set; }
    }

   
    public class CompleteFightDto
    {
        public string? CompletionNotes { get; set; }
        
    }
}