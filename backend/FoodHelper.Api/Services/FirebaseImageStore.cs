using FoodHelper.Api.Options;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace FoodHelper.Api.Services;

public sealed class FirebaseImageStore(StorageClient storageClient, IOptions<StorageOptions> options) : IImageStore
{
    private readonly string _bucketName = NormalizeBucketName(options.Value.BucketName);

    public async Task<string> UploadAsync(Stream imageStream, string contentType, string recipeId, CancellationToken cancellationToken)
    {
        var extension = contentType.Split('/').LastOrDefault() ?? "bin";
        if (extension == "jpeg") extension = "jpg";

        var token = Guid.NewGuid().ToString("n");
        var objectName = $"recipe-images/{recipeId}/{token}.{extension}";

        var obj = await storageClient.UploadObjectAsync(
            _bucketName,
            objectName,
            contentType,
            imageStream,
            new UploadObjectOptions { PredefinedAcl = null },
            cancellationToken
        ).ConfigureAwait(false);

        obj.Metadata ??= new Dictionary<string, string>();
        obj.Metadata["firebaseStorageDownloadTokens"] = token;
        await storageClient.PatchObjectAsync(obj, cancellationToken: cancellationToken).ConfigureAwait(false);

        var encodedPath = Uri.EscapeDataString(objectName);
        return $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedPath}?alt=media&token={token}";
    }

    private static string NormalizeBucketName(string configuredBucketName)
    {
        if (string.IsNullOrWhiteSpace(configuredBucketName))
        {
            return string.Empty;
        }

        var bucketName = configuredBucketName.Trim();
        if (bucketName.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        {
            bucketName = bucketName[5..];
        }

        return bucketName.TrimEnd('/');
    }
}
