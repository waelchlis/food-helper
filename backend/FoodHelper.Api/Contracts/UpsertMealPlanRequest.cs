using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class UpsertMealPlanRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}
