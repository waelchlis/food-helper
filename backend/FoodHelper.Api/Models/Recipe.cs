using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

[FirestoreData]
public sealed class Recipe
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("name")]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty("description")]
    public string Description { get; set; } = string.Empty;

    [FirestoreProperty("servings")]
    public int Servings { get; set; }

    [FirestoreProperty("prepTime")]
    public int PrepTime { get; set; }

    [FirestoreProperty("cookTime")]
    public int CookTime { get; set; }

    [FirestoreProperty("ingredients")]
    public List<RecipeIngredient> Ingredients { get; set; } = [];

    [FirestoreProperty("instructions")]
    public List<string> Instructions { get; set; } = [];

    [FirestoreProperty("image")]
    public string? Image { get; set; }

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
