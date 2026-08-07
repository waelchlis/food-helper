using FoodHelper.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodHelper.Api.Tests.Controllers;

/// <summary>
/// Boots the real app with the real controllers/authorization policies wired up, but:
///  - forces every store to the in-memory implementation (never touches real Firestore/Storage,
///    regardless of what's in appsettings.Development.json on the machine running the tests), and
///  - swaps real Google JWT validation for TestAuthHandler so tests can authenticate as an
///    arbitrary subject without a real ID token.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Non-"Development" environment so Program.cs loads appsettings.Production.json (which
        // ships with an empty Firebase:ProjectId) rather than the developer's real project config.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firebase:ProjectId"] = "",
                ["Storage:BucketName"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    public HttpClient CreateAuthenticatedClient(string subject, string? email = null, string? name = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Sub", subject);
        if (email is not null) client.DefaultRequestHeaders.Add("X-Test-Email", email);
        if (name is not null) client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }

    public async Task MakeAdminAsync(string subject, string email)
    {
        using var scope = Services.CreateScope();
        var adminStore = scope.ServiceProvider.GetRequiredService<IAdminStore>();
        await adminStore.AddByEmailAsync(email, CancellationToken.None);
        await adminStore.TryLinkSubjectAsync(subject, email, CancellationToken.None);
    }
}
