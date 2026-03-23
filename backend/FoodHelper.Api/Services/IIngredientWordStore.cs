using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IIngredientWordStore
{
    Task<IReadOnlyList<IngredientWord>> GetAllAsync(CancellationToken cancellationToken);
    Task<IngredientWord> AddAsync(string name, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
    Task SeedAsync(IReadOnlyList<string> words, CancellationToken cancellationToken);
}
