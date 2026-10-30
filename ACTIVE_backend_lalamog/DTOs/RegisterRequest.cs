using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class RegisterRequest
    {
        public RegisterRequest()
        {
        }

        public RegisterRequest(string email, string password, string nickname)
        {
            Email = email;
            Password = password;
            Nickname = nickname;
        }

        [Required]
        [EmailAddress]
        [MaxLength(254)]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(128, MinimumLength = 10)]
        public string Password { get; set; } = null!;

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Nickname { get; set; } = null!;
    }
}
