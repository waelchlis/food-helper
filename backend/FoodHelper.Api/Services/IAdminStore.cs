using FoodHelper.Api.Models;

namespace FoodHelper.Api.Services;

public interface IAdminStore
{
    Task<bool> IsAdminAsync(string googleSubjectId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminUser> AddByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> RemoveAsync(string id, CancellationToken cancellationToken);
    Task<bool> TryLinkSubjectAsync(string googleSubjectId, string email, CancellationToken cancellationToken);
}
