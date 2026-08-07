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

    [FirestoreProperty("tips")]
    public List<string> Tips { get; set; } = [];

    [FirestoreProperty("images")]
    public List<string> Images { get; set; } = [];

    /// <summary>
    /// Back-compat only: recipes written before the single-Image-to-Images migration still have
    /// data under the old "image" field. FirestoreRecipeStore.Map lazily migrates this into
    /// Images on read so no explicit migration script needs to run. New writes never populate this.
    /// </summary>
    [FirestoreProperty("image")]
    public string? LegacyImage { get; set; }

    [FirestoreProperty("categoryId")]
    public string? CategoryId { get; set; }

    [FirestoreProperty("dietType")]
    public string? DietType { get; set; }

    /// <summary>Public note from the recipe's creator/admin, visible to everyone viewing the recipe.</summary>
    [FirestoreProperty("note")]
    public string? Note { get; set; }

    [FirestoreProperty("creatorName")]
    public string CreatorName { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [FirestoreProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}
