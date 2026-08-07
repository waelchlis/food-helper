using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

[FirestoreData]
public sealed class MealEntry
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Date in YYYY-MM-DD format.</summary>
    [FirestoreProperty("date")]
    public string Date { get; set; } = string.Empty;

    /// <summary>"recipe" or "custom"</summary>
    [FirestoreProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>Set when Type is "recipe". References an existing recipe by ID.</summary>
    [FirestoreProperty("recipeId")]
    public string? RecipeId { get; set; }

    /// <summary>Denormalized recipe name so the calendar renders without loading all recipes.</summary>
    [FirestoreProperty("recipeName")]
    public string? RecipeName { get; set; }

    /// <summary>Denormalized recipe image URL for preview.</summary>
    [FirestoreProperty("recipeImage")]
    public string? RecipeImage { get; set; }

    /// <summary>
    /// Planned serving count for this entry, independent of the recipe's own base Servings.
    /// Defaults to the recipe's Servings when the entry is added, but can be overridden
    /// (e.g. "make this for 6 people even though it's a 4-serving recipe"). Used to scale
    /// ingredient amounts when generating a shopping list from the plan.
    /// </summary>
    [FirestoreProperty("servings")]
    public int? Servings { get; set; }

    /// <summary>Set when Type is "custom".</summary>
    [FirestoreProperty("customText")]
    public string? CustomText { get; set; }

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
