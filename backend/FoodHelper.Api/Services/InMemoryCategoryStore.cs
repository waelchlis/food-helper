using System.Collections.Concurrent;
using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public sealed class InMemoryCategoryStore : ICategoryStore
{
    private readonly ConcurrentDictionary<string, Category> _categories = new();

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        var items = _categories.Values
            .OrderBy(c => c.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Category>>(items);
    }

    public Task<Category> AddAsync(string name, CancellationToken cancellationToken)
    {
        var category = new Category
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name.Trim(),
        };
        _categories[category.Id] = category;
        return Task.FromResult(category);
    }

    public Task<Category?> RenameAsync(string id, string newName, CancellationToken cancellationToken)
    {
        if (!_categories.TryGetValue(id, out var existing))
            return Task.FromResult<Category?>(null);

        var updated = new Category { Id = id, Name = newName.Trim() };
        _categories[id] = updated;
        return Task.FromResult<Category?>(updated);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(_categories.TryRemove(id, out _));
    }
}
