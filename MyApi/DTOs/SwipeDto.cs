using System.ComponentModel.DataAnnotations;

namespace MyApi.DTOs
{
    public class SwipeDto
    {
        [Required]
        public Guid SwipeeId { get; set; }
        
        [Required]
        [RegularExpression("right|left", ErrorMessage = "Direction must be 'right' or 'left'")]
        public string Direction { get; set; } = "right";
    }


    // One call to fetch all profiles 
    public class SwipeProfilesDto
    {
        public List<ProfileDto> MatchedProfiles { get; set; } = new List<ProfileDto>();
        public List<ProfileDto> AvailableProfiles { get; set; } = new List<ProfileDto>();
    }




    public class SwipeResultDto
    {
        public ProfileDto? Swiper { get; set; }
        public bool IsMatch { get; set; }
        public long? MatchId { get; set; }
        public string Message { get; set; } = string.Empty;
        public ProfileDto? MatchedUser { get; set; }
    }

    public class MatchDto
    {
        public ProfileDto? User1 { get; set; }
        public ProfileDto? User2 { get; set; }
        public DateTime CreatedAt { get; set; }

        public long Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PotentialMatchDto
    {
        public Guid UserId { get; set; }
        public string? Nickname { get; set; }
        public string? Bio { get; set; }
        public string? FightingStyle { get; set; }
        public int? ExperienceLevel { get; set; }
        public LocationDto? Location { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public double Distance { get; set; } // Distance in kilometers
    }
}