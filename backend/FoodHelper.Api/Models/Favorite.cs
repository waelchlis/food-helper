using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

[FirestoreData]
public sealed class Favorite
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("ownerKey")]
    public string OwnerKey { get; set; } = string.Empty;

    [FirestoreProperty("recipeId")]
    public string RecipeId { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
