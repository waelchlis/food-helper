using Google.Cloud.Firestore;

namespace FoodHelper.Api.Models;

[FirestoreData]
public sealed class AdminUser
{
    public string Id { get; set; } = string.Empty;

    [FirestoreProperty("googleSubjectId")]
    public string GoogleSubjectId { get; set; } = string.Empty;

    [FirestoreProperty("email")]
    public string Email { get; set; } = string.Empty;
}
