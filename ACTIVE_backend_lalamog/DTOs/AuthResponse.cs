namespace Lalamog.Api.DTOs
{
    public class AuthResponse
    {
        public List<string> Roles { get; set; } = [];
        public List<string> Permissions { get; set; } = [];
        public Guid UserId { get; set; }
    }
}
