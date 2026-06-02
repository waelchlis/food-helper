using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class AddCollaboratorRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;
}
