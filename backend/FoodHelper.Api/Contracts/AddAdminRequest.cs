using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class AddAdminRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}
