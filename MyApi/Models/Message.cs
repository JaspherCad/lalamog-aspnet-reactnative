using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace MyApi.Models
{
    public class Message
    {
        [Key]
        public long Id { get; set; } // Keep as long to match existing database

        public long MatchId { get; set; }
        public Match? Match { get; set; } = null!;






        public Guid SenderId { get; set; }
        public ApplicationUser? Sender { get; set; }

        public Guid ReceiverId { get; set; }
        public ApplicationUser? Receiver { get; set; }

        public string Content { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Read { get; set; } = false;

        



    }
}
