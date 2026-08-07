using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

/// <summary>
/// Admin-only audit-trail snapshot of a recipe's state immediately before an edit was applied.
/// Retention is capped at the 5 most recent revisions per recipe (see IRecipeRevisionStore).
/// </summary>
[FirestoreData]
public sealed class RecipeRevision
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("recipeId")]
    public string RecipeId { get; set; } = string.Empty;

    [FirestoreProperty("snapshot")]
    public Recipe Snapshot { get; set; } = new();

    [FirestoreProperty("editorSubject")]
    public string EditorSubject { get; set; } = string.Empty;

    [FirestoreProperty("editorName")]
    public string EditorName { get; set; } = string.Empty;

    [FirestoreProperty("createdAt")]
    public DateTime CreatedAt { get; set; }
}
