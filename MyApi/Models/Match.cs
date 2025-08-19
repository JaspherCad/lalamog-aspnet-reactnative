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

    }


}
