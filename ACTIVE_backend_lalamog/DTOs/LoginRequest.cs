using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class LoginRequest
    {
        public LoginRequest()
        {
        }

        public LoginRequest(string email, string password)
        {
            Email = email;
            Password = password;
        }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;
    }
}
