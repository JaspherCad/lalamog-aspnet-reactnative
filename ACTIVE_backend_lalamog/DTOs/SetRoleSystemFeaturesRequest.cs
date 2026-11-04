using System.ComponentModel.DataAnnotations;

namespace Lalamog.Api.DTOs;

public class SetRoleSystemFeaturesRequest
{
    [Required, MaxLength(100)]
    public List<int> SystemFeatureIds { get; set; } = null!;
}
