namespace FoodHelper.Api.Services;

public interface IImageStore
{
    Task<string> UploadAsync(Stream imageStream, string contentType, string recipeId, CancellationToken cancellationToken);
}
