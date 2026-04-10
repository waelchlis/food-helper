using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface ICategoryStore
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
    Task<Category> AddAsync(string name, CancellationToken cancellationToken);
    Task<Category?> RenameAsync(string id, string newName, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
