using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class MessageRequest
    {
        public MessageRequest()
        {
        }

        public MessageRequest(string content)
        {
            Content = content;
        }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = null!;
    }
}
