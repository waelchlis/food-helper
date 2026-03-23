using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

[FirestoreData]
public sealed class RecipeIngredient
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("name")]
    public string Name { get; set; } = string.Empty;

    [FirestoreProperty("amount")]
    public double Amount { get; set; }

    [FirestoreProperty("unit")]
    public string Unit { get; set; } = string.Empty;
}
