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

        public long MatchId { get; set; }

        public DateTimeOffset ScheduledAt { get; set; }

        [Required]
        [MaxLength(200)]
        public string Location { get; set; } = null!;

        [Required]
        [MaxLength(300)]
        public string Address { get; set; } = null!;
    }
}
