using FoodHelper.Api.Options;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace FoodHelper.Api.Services;

/// <summary>
/// Periodically purges objects under the trash/ prefix (soft-deleted recipe images, see
/// FirebaseImageStore.DeleteAsync) once they're older than the retention window. Registered
/// only when Firebase Storage is actually configured — see Program.cs.
/// </summary>
public sealed class TrashCleanupService(
    StorageClient storageClient,
    IOptions<StorageOptions> options,
    ILogger<TrashCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(30);
    private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

    private readonly string _bucketName = options.Value.BucketName.Trim().TrimStart("gs://".ToCharArray());

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger the first run slightly after startup rather than racing app initialization.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await PurgeExpiredTrashAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(RunInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }

    private async Task PurgeExpiredTrashAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            return;
        }

        var cutoff = DateTimeOffset.UtcNow - RetentionWindow;
        var purged = 0;

        try
        {
            var objects = storageClient.ListObjectsAsync(_bucketName, FirebaseImageStore.TrashPrefix);
            await foreach (var obj in objects.WithCancellation(cancellationToken))
            {
                if (obj.TimeCreatedDateTimeOffset is { } createdAt && createdAt < cutoff)
                {
                    await storageClient.DeleteObjectAsync(_bucketName, obj.Name, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    purged++;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Trash cleanup pass failed");
            return;
        }

        if (purged > 0)
        {
            logger.LogInformation("Trash cleanup purged {Count} object(s) older than {RetentionDays} days", purged, RetentionWindow.TotalDays);
        }
    }
}
