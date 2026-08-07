namespace FoodHelper.Api.Services;

public interface IImageStore
{
    Task<string> UploadAsync(Stream imageStream, string contentType, string recipeId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a previously-uploaded image (moves it to a trash/ prefix rather than
    /// deleting outright, so an accidental removal/replace can still be recovered within the
    /// retention window). No-op for stores that don't persist images externally.
    /// </summary>
    Task DeleteAsync(string imageUrl, CancellationToken cancellationToken);
}
