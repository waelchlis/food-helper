using FoodHelper.Api.Authorization;
using FoodHelper.Api.Data;
using FoodHelper.Api.Options;
using FoodHelper.Api.Services;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Load local settings in Development and production settings for all other environments.
var environmentSettingsFile = builder.Environment.IsDevelopment()
    ? "appsettings.Development.json"
    : "appsettings.Production.json";
builder.Configuration.AddJsonFile(
    environmentSettingsFile,
    optional: builder.Environment.IsDevelopment(),
    reloadOnChange: builder.Environment.IsDevelopment());

builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection("Firebase"));
builder.Services.Configure<GoogleOidcOptions>(builder.Configuration.GetSection("GoogleOidc"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", cors =>
    {
        cors.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var googleOidc = builder.Configuration.GetSection("GoogleOidc").Get<GoogleOidcOptions>() ?? new GoogleOidcOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = googleOidc.Authority;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = googleOidc.ValidIssuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(googleOidc.Audience),
            ValidAudience = googleOidc.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            NameClaimType = "name"
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.Requirements.Add(new AdminRequirement()));
});
builder.Services.AddSingleton<IAuthorizationHandler, AdminRequirementHandler>();

var firebase = builder.Configuration.GetSection("Firebase").Get<FirebaseOptions>() ?? new FirebaseOptions();
if (!string.IsNullOrWhiteSpace(firebase.GoogleApplicationCredentialsPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebase.GoogleApplicationCredentialsPath);
}

if (!string.IsNullOrWhiteSpace(firebase.ProjectId))
{
    try
    {
        var firestore = FirestoreDb.Create(firebase.ProjectId);
        builder.Services.AddSingleton(firestore);
        builder.Services.AddSingleton<IRecipeStore, FirestoreRecipeStore>();
        builder.Services.AddSingleton<IShoppingListStore, FirestoreShoppingListStore>();
        builder.Services.AddSingleton<IAdminStore, FirestoreAdminStore>();
        builder.Services.AddSingleton<IIngredientWordStore, FirestoreIngredientWordStore>();
        builder.Services.AddSingleton<ICategoryStore, FirestoreCategoryStore>();

        var storageBucket = builder.Configuration.GetValue<string>("Storage:BucketName");
        if (!string.IsNullOrWhiteSpace(storageBucket))
        {
            var storageClient = StorageClient.Create();
            builder.Services.AddSingleton(storageClient);
            builder.Services.AddSingleton<IImageStore, FirebaseImageStore>();
        }
        else
        {
            builder.Services.AddSingleton<IImageStore, InMemoryImageStore>();
        }
    }
    catch (Exception ex)
    {
        builder.Logging.AddConsole();
        builder.Services.AddSingleton<IRecipeStore, InMemoryRecipeStore>();
        builder.Services.AddSingleton<IShoppingListStore, InMemoryShoppingListStore>();
        builder.Services.AddSingleton<IAdminStore, InMemoryAdminStore>();
        builder.Services.AddSingleton<IImageStore, InMemoryImageStore>();
        builder.Services.AddSingleton<IIngredientWordStore, InMemoryIngredientWordStore>();
        builder.Services.AddSingleton<ICategoryStore, InMemoryCategoryStore>();
        Console.WriteLine($"Firestore initialization failed: {ex.Message}. Falling back to in-memory stores.");
    }
}
else
{
    builder.Services.AddSingleton<IRecipeStore, InMemoryRecipeStore>();
    builder.Services.AddSingleton<IShoppingListStore, InMemoryShoppingListStore>();
    builder.Services.AddSingleton<IAdminStore, InMemoryAdminStore>();
    builder.Services.AddSingleton<IImageStore, InMemoryImageStore>();
    builder.Services.AddSingleton<IIngredientWordStore, InMemoryIngredientWordStore>();
    builder.Services.AddSingleton<ICategoryStore, InMemoryCategoryStore>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Food Helper API v1");
        options.RoutePrefix = "swagger";
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

// Seed default ingredient words
using (var scope = app.Services.CreateScope())
{
    var wordStore = scope.ServiceProvider.GetRequiredService<IIngredientWordStore>();
    try
    {
        await wordStore.SeedAsync(DefaultIngredients.German, CancellationToken.None);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ingredient seeding failed: {ex.Message}");
    }
}

await app.RunAsync();
