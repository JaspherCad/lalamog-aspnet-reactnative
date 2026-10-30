using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class SwipeRequest
    {
        public SwipeRequest()
        {
        }

        public SwipeRequest(Guid targetId, string direction)
        {
            TargetId = targetId;
            Direction = direction;
        }

        public Guid TargetId { get; set; }

        [Required]
        [RegularExpression("^(like|pass)$")]
        public string Direction { get; set; } = null!;
    }
}
