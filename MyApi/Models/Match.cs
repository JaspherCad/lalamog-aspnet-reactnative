using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;
using NpgsqlTypes;

namespace MyApi.Models
{
    public class Match
    {
        public long Id { get; set; }

        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Status constraint: 'pending', 'active', 'ended'
        public string Status { get; set; } = "pending";

        // Navigation properties
        public ApplicationUser User1 { get; set; } = null!;
        public ApplicationUser User2 { get; set; } = null!;

        public ICollection<Message> ListOfMessages { get; set; } = new List<Message>();

        //#note: List of FightSchedules (ONE TO MANY) -- just reverse link
        public ICollection<FightSchedule> FightSchedules { get; set; } = new List<FightSchedule>();

        // Helper methods for head-to-head stats (calculated from FightSchedules)
        public int GetWinsForUser(Guid userId)
        {
            return FightSchedules?.Count(fs => fs.Status == "completed" && fs.WinnerId == userId) ?? 0;
        }

        public int GetLossesForUser(Guid userId)
        {
            var opponentId = userId == User1Id ? User2Id : User1Id;
            return FightSchedules?.Count(fs => fs.Status == "completed" && fs.WinnerId == opponentId) ?? 0;
        }

        public int GetDrawsCount()
        {
            return FightSchedules?.Count(fs => fs.Status == "completed" && fs.FightResult == "draw") ?? 0;
        }

        public int GetTotalCompletedFights()
        {
            return FightSchedules?.Count(fs => fs.Status == "completed" && fs.ResultRecordedAt != null) ?? 0;
        }

        public FightSchedule? GetLatestCompletedFight()
        {
            return FightSchedules?
                .Where(fs => fs.Status == "completed" && fs.ResultRecordedAt != null)
                .OrderByDescending(fs => fs.ScheduledDateTime)
                .FirstOrDefault();
        }

    }


}
