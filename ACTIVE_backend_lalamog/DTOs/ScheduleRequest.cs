using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class ScheduleRequest
    {
        public ScheduleRequest()
        {
        }

        public ScheduleRequest(long matchId, DateTimeOffset scheduledAt, string location, string address)
        {
            MatchId = matchId;
            ScheduledAt = scheduledAt;
            Location = location;
            Address = address;
        }

        public long? GymId { get; set; }
        public string? GymVersion { get; set; }
        public long MatchId { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }

        [MaxLength(200)]
        public string Location { get; set; } = "";

        [MaxLength(300)]
        public string Address { get; set; } = "";
        public MapLocation? MapLocation { get; set; }
    }
}
