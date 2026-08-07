using System.ComponentModel.DataAnnotations;

namespace FoodHelper.Api.Contracts;

public sealed class GenerateShoppingListRequest
{
    /// <summary>Inclusive range start, YYYY-MM-DD.</summary>
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "From must be in YYYY-MM-DD format.")]
    public string From { get; set; } = string.Empty;

    /// <summary>Inclusive range end, YYYY-MM-DD.</summary>
    [Required]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "To must be in YYYY-MM-DD format.")]
    public string To { get; set; } = string.Empty;
}
