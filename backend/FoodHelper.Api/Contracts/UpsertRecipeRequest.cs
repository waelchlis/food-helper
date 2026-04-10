using System.ComponentModel.DataAnnotations;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Contracts;

public sealed class UpsertRecipeRequest
{
    [Required]
    [MaxLength(160)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Range(1, 500)]
    public int Servings { get; init; } = 1;

    [Range(0, 6000)]
    public int PrepTime { get; init; }

    [Range(0, 6000)]
    public int CookTime { get; init; }

    [Required]
    public List<RecipeIngredient> Ingredients { get; init; } = [];

    [Required]
    public List<string> Instructions { get; init; } = [];

    [MaxLength(2048)]
    public string? Image { get; init; }

    public string? CategoryId { get; init; }
}
