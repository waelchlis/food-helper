using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class AddMealEntryRequest
{
    /// <summary>Date in YYYY-MM-DD format.</summary>
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Date must be in YYYY-MM-DD format.")]
    public string Date { get; set; } = string.Empty;

    /// <summary>"recipe" or "custom"</summary>
    [Required]
    [RegularExpression("^(recipe|custom)$", ErrorMessage = "Type must be 'recipe' or 'custom'.")]
    public string Type { get; set; } = string.Empty;

    public string? RecipeId { get; set; }

    [MaxLength(160)]
    public string? RecipeName { get; set; }

    [MaxLength(2048)]
    public string? RecipeImage { get; set; }

    /// <summary>Optional override; defaults to the referenced recipe's own Servings when omitted.</summary>
    [Range(1, 500)]
    public int? Servings { get; set; }

    [MaxLength(500)]
    public string? CustomText { get; set; }
}
