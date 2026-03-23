namespace FoodHelper.Api.Options;

public sealed class GoogleOidcOptions
{
    public string Authority { get; init; } = "https://accounts.google.com";
    public string ValidIssuer { get; init; } = "https://accounts.google.com";
    public string Audience { get; init; } = "replace-with-google-client-id.apps.googleusercontent.com";
}
