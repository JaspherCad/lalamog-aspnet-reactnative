using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs
{
    public class MessageDto
    {

        public long? Id { get; set; }
        [Required]
        public long MatchId { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool Read { get; set; } = false;

    }


}