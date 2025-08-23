using System;

namespace MyApi.DTOs
{
   
    public class FightResultDto
    {
        public long FightScheduleId { get; set; }
        public string FightResult { get; set; } = string.Empty; // "user1_win", "user2_win", "draw", "no_contest"
        public Guid? WinnerId { get; set; } // null if draw/no_contest
        public string? WinMethod { get; set; } // "KO", "TKO", "Decision", "Submission", etc.
        public int? FightDurationMinutes { get; set; }
        public string? ResultNotes { get; set; }
        







        // Optional post-fight ratings/feedback
        public int? User1Rating { get; set; }
        public int? User2Rating { get; set; }
        public string? User1Feedback { get; set; }
        public string? User2Feedback { get; set; }
    }

    
    public class UserStatsDto
    {
        public Guid UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        
        // Overall statistics
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalDraws { get; set; }
        public int TotalFights { get; set; }
        public double WinPercentage { get; set; }
        public int Rating { get; set; }
        
        // Streak information
        public int CurrentWinStreak { get; set; }
        public int CurrentLossStreak { get; set; }
        public int BestWinStreak { get; set; }
        
        // Profile info
        public int? ExperienceLevel { get; set; }
        public string? FightingStyle { get; set; }
    }

    public class FightConfirmationDto
    {
        public bool Confirmed { get; set; }
        public string? DisputeReason { get; set; }
        public string? AdditionalNotes { get; set; }
    }
    public class HeadToHeadStatsDto
    {
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
        
        public string User1Nickname { get; set; } = string.Empty;
        public string User2Nickname { get; set; } = string.Empty;
        
        public string? User1ProfilePicture { get; set; }
        public string? User2ProfilePicture { get; set; }
        
        // Head-to-head record
        public int User1Wins { get; set; }
        public int User2Wins { get; set; }
        public int Draws { get; set; }
        public int TotalFights { get; set; }
        
        // From requesting user's perspective
        public int MyWins { get; set; }
        public int MyLosses { get; set; }
        public double MyWinPercentage { get; set; }
        
        // Recent fight info
        public DateTime? LastFightDate { get; set; }
        public Guid? LastWinnerId { get; set; }
        public string? LastWinMethod { get; set; }
        public string LastFightResult { get; set; } = string.Empty; // "Win", "Loss", "Draw", "No fights yet"
    }

    public class FightHistoryDto
    {
        public long FightScheduleId { get; set; }
        public DateTime FightDate { get; set; }
        public Guid OpponentId { get; set; }
        public string OpponentNickname { get; set; } = string.Empty;
        public string? OpponentProfilePicture { get; set; }
        
        public string Result { get; set; } = string.Empty; // "Win", "Loss", "Draw"
        public string? WinMethod { get; set; }
        public int? FightDurationMinutes { get; set; }
        
        public string LocationName { get; set; } = string.Empty;
        public string LocationAddress { get; set; } = string.Empty;
        
        // Rating changes
        public int? RatingBefore { get; set; }
        public int? RatingAfter { get; set; }
        public int? RatingChange { get; set; }
        
        // Feedback
        public string? MyFeedback { get; set; }
        public string? OpponentFeedback { get; set; }
        public int? MyRating { get; set; }
        public int? OpponentRating { get; set; }
    }

    
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; }
        public Guid UserId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public int Rating { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalFights { get; set; }
        public double WinPercentage { get; set; }
        public int CurrentWinStreak { get; set; }
        public string? FightingStyle { get; set; }
        public int? ExperienceLevel { get; set; }
    }
}
