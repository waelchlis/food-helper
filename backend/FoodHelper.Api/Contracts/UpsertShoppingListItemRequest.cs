using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class UpsertShoppingListItemRequest
{
    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 100000)]
    public double Amount { get; init; }

    [Required]
    [MaxLength(64)]
    public string Unit { get; init; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; init; } = string.Empty;
}
