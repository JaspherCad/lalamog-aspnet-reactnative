using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class CancelRequest
    {
        public CancelRequest()
        {
        }

        public CancelRequest(string reason)
        {
            Reason = reason;
        }

        [Required]
        [MaxLength(500)]
        public string Reason { get; set; } = null!;
    }
}
