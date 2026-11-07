using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs
{
    public class ConfirmRequest
    {
        public ConfirmRequest()
        {
        }

        public ConfirmRequest(bool accepted, string emergencyContactName, string emergencyContactPhone)
        {
            Accepted = accepted;
            EmergencyContactName = emergencyContactName;
            EmergencyContactPhone = emergencyContactPhone;
        }

        public bool AllowPublicIdentity { get; set; }
        public bool Accepted { get; set; }

        [Required]
        [MaxLength(100)]
        public string EmergencyContactName { get; set; } = null!;

        [Required]
        [MaxLength(40)]
        public string EmergencyContactPhone { get; set; } = null!;
    }
}
