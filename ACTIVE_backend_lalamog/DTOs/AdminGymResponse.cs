namespace Lalamog.Api.DTOs;
public class AdminGymResponse
{
    public GymResponse Gym { get; set; } = new();
    public Guid? OwnerUserId { get; set; }
}
