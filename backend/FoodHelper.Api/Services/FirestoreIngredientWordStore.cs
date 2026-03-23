using FoodHelper.Api.Models;
using Google.Cloud.Firestore;

namespace FoodHelper.Api.Services;

public sealed class FirestoreIngredientWordStore(FirestoreDb db) : IIngredientWordStore
{
    private readonly CollectionReference _words = db.Collection("ingredientWords");

    public async Task<IReadOnlyList<IngredientWord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _words
            .OrderBy("name")
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        return snapshot.Documents
            .Select(Map)
            .ToList();
    }

    public async Task<IngredientWord> AddAsync(string name, CancellationToken cancellationToken)
    {
        var word = new IngredientWord
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name.Trim(),
        };

        await _words.Document(word.Id).SetAsync(word, cancellationToken: cancellationToken).ConfigureAwait(false);
        return word;
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var doc = _words.Document(id);
        var snapshot = await doc.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (!snapshot.Exists)
        {
            return false;
        }

        await doc.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task SeedAsync(IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        var existing = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        var existingNames = existing.Select(w => w.Name.ToLowerInvariant()).ToHashSet();

        var toAdd = words
            .Where(w => !existingNames.Contains(w.ToLowerInvariant()))
            .ToList();

        if (toAdd.Count == 0) return;

        const int batchSize = 400;
        for (var i = 0; i < toAdd.Count; i += batchSize)
        {
            var batch = db.StartBatch();
            foreach (var name in toAdd.Skip(i).Take(batchSize))
            {
                var id = Guid.NewGuid().ToString("n");
                var word = new IngredientWord { Id = id, Name = name };
                batch.Set(_words.Document(id), word);
            }
            await batch.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static IngredientWord Map(DocumentSnapshot snapshot)
    {
        var word = snapshot.ConvertTo<IngredientWord>();
        if (string.IsNullOrWhiteSpace(word.Id))
        {
            word.Id = snapshot.Id;
        }
        return word;
    }
}
