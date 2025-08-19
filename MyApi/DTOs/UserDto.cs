using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? FullName { get; set; }
        public DateTime? BirthDate { get; set; }
    }



    public class LocationDto
    {
        public double X { get; set; } // Longitude
        public double Y { get; set; } // Latitude
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string JwtToken { get; set; } = string.Empty; 
    }

    public class ProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Nickname { get; set; }
        public string? Bio { get; set; }
        public LocationDto? Location { get; set; }
        public string? FightingStyle { get; set; }
        public int? ExperienceLevel { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Availability? Availability { get; set; }

        public string? JwtToken { get; set; }
    }

    public class UpdateProfileDto
    {
        [StringLength(50)]
        public string? Nickname { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }

        public LocationDto? Location { get; set; }

        [StringLength(100)]
        public string? FightingStyle { get; set; }

        // Experience Level: 1 = Beginner, 2 = Intermediate, 3 = Expert
        [Range(1, 3, ErrorMessage = "Experience level must be between 1 (Beginner) and 3 (Expert)")]
        public int? ExperienceLevel { get; set; }

        [StringLength(100)]
        public string? ProfilePictureUrl { get; set; }

        public Availability? Availability { get; set; }
    }

    public class Availability
    {
        public string[]? Days { get; set; }
        public string? Time { get; set; }
    }
}
