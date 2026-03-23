namespace FoodHelper.Api.Options;

public sealed class FirebaseOptions
{
    public string ProjectId { get; init; } = string.Empty;
    public string GoogleApplicationCredentialsPath { get; init; } = string.Empty;
}
