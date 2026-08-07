using FoodHelper.Api.Options;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodHelper.Api.Services;

public sealed class FirebaseImageStore(StorageClient storageClient, IOptions<StorageOptions> options, ILogger<FirebaseImageStore> logger) : IImageStore
{
    public const string TrashPrefix = "trash/";

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

    public async Task DeleteAsync(string imageUrl, CancellationToken cancellationToken)
    {
        var objectName = TryExtractObjectName(imageUrl);
        if (objectName is null || objectName.StartsWith(TrashPrefix, StringComparison.Ordinal))
        {
            return;
        }

        var trashObjectName = TrashPrefix + objectName;

        try
        {
            await storageClient.CopyObjectAsync(_bucketName, objectName, _bucketName, trashObjectName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            await storageClient.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Best-effort: a missing/already-moved source object shouldn't block the caller's
            // primary operation (e.g. replacing a recipe's image or deleting the recipe itself).
            logger.LogWarning(ex, "Failed to soft-delete image object {ObjectName} to trash", objectName);
        }
    }

    /// <summary>Extracts the "recipe-images/{recipeId}/{token}.{ext}" object path back out of a download URL produced by UploadAsync.</summary>
    private static string? TryExtractObjectName(string imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // Path shape: /v0/b/{bucket}/o/{url-encoded object name}
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var oIndex = Array.IndexOf(segments, "o");
        if (oIndex < 0 || oIndex + 1 >= segments.Length)
        {
            return null;
        }

        return Uri.UnescapeDataString(segments[oIndex + 1]);
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
