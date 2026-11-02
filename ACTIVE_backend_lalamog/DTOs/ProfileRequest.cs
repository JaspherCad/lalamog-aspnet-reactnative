using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class ProfileRequest
    {
        public ProfileRequest()
        {
        }

        public ProfileRequest(string nickname, string bio, string fightingStyle, int experienceLevel, string location, string availability)
        {
            Nickname = nickname;
            Bio = bio;
            FightingStyle = fightingStyle;
            ExperienceLevel = experienceLevel;
            Location = location;
            Availability = availability;
        }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public string Nickname { get; set; } = null!;

        [StringLength(50, MinimumLength = 2)]
        [RegularExpression(@"[A-Za-z0-9][A-Za-z0-9._-]*", ErrorMessage = "Use letters, numbers, dots, underscores or hyphens for your fighter name.")]
        public string? FighterName { get; set; }

        [MaxLength(500)]
        public string Bio { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string FightingStyle { get; set; } = null!;

        [Range(1, 3)]
        public int ExperienceLevel { get; set; }

        [MaxLength(200)]
        public string Location { get; set; } = null!;

        [MaxLength(200)]
        public string Availability { get; set; } = null!;
        public MapLocation? MapLocation { get; set; }
    }
}
