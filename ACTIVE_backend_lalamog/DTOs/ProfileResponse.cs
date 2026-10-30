namespace Lalamog.Api.DTOs
{
    public class ProfileResponse
    {
        public Guid UserId { get; set; }

        public string Nickname { get; set; } = string.Empty;

        public string Bio { get; set; } = string.Empty;

        public string FightingStyle { get; set; } = string.Empty;

        public int ExperienceLevel { get; set; }

        public string Location { get; set; } = string.Empty;

        public string Availability { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
    }
}
