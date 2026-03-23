namespace FoodHelper.Api.Services;

public sealed class InMemoryImageStore : IImageStore
{
    public async Task<string> UploadAsync(Stream imageStream, string contentType, string recipeId, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return $"data:{contentType};base64,{base64}";
    }
}
