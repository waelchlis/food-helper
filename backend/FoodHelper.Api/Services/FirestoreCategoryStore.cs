using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreCategoryStore(FirestoreDb db) : ICategoryStore
{
    private readonly CollectionReference _categories = db.Collection("categories");

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _categories
            .OrderBy("name")
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents.Select(Map).ToList();
    }

    public async Task<Category> AddAsync(string name, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name.Trim(),
        };

        await _categories.Document(category.Id).SetAsync(category, cancellationToken: cancellationToken).ConfigureAwait(false);
        return category;
    }

    public async Task<Category?> RenameAsync(string id, string newName, CancellationToken cancellationToken)
    {
        var doc = _categories.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists) return null;

        var updated = new Category { Id = id, Name = newName.Trim() };
        await doc.SetAsync(updated, cancellationToken: cancellationToken).ConfigureAwait(false);
        return updated;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var doc = _categories.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists) return false;

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static Category Map(DocumentSnapshot snapshot)
    {
        var category = snapshot.ConvertTo<Category>();
        if (string.IsNullOrWhiteSpace(category.Id))
            category.Id = snapshot.Id;
        return category;
    }
}
