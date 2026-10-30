using System;
using System.ComponentModel.DataAnnotations;

namespace MyApi.Models
{
    public class SwipeAction
    {
        [Key]
        public long Id { get; set; } // Keep as long to match existing database

        public Guid SwiperId { get; set; }
        public ApplicationUser? Swiper { get; set; }

        public Guid SwipeeId { get; set; }
        public ApplicationUser? Swipee { get; set; }

        public string Direction { get; set; } = string.Empty; // "right" or "left"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
