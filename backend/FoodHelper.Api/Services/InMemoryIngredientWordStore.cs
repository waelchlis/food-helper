using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryIngredientWordStore : IIngredientWordStore
{
    private readonly ConcurrentDictionary<string, IngredientWord> _words = new();

    public Task<IReadOnlyList<IngredientWord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = _words.Values
            .OrderBy(w => w.Name)
            .ToList();

        return Task.FromResult<IReadOnlyList<IngredientWord>>(items);
    }

    public Task<IngredientWord> AddAsync(string name, CancellationToken cancellationToken)
    {
        var word = new IngredientWord
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name.Trim(),
        };
        _words[word.Id] = word;
        return Task.FromResult(word);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_words.TryRemove(id, out _));
    }

    public Task SeedAsync(IReadOnlyList<string> words, CancellationToken cancellationToken)
    {
        var existingNames = _words.Values.Select(w => w.Name.ToLowerInvariant()).ToHashSet();
        foreach (var name in words.Where(w => !existingNames.Contains(w.ToLowerInvariant())))
        {
            var id = Guid.NewGuid().ToString("n");
            _words[id] = new IngredientWord { Id = id, Name = name };
        }
        return Task.CompletedTask;
    }
}
