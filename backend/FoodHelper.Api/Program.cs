using FoodHelper.Api.Options;
using FoodHelper.Api.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection("Firebase"));
builder.Services.Configure<GoogleOidcOptions>(builder.Configuration.GetSection("GoogleOidc"));

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
builder.Services.AddAuthorization();

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
    }
    catch (Exception ex)
    {
        builder.Logging.AddConsole();
        builder.Services.AddSingleton<IRecipeStore, InMemoryRecipeStore>();
        builder.Services.AddSingleton<IShoppingListStore, InMemoryShoppingListStore>();
        Console.WriteLine($"Firestore initialization failed: {ex.Message}. Falling back to in-memory stores.");
    }
}
else
{
    builder.Services.AddSingleton<IRecipeStore, InMemoryRecipeStore>();
    builder.Services.AddSingleton<IShoppingListStore, InMemoryShoppingListStore>();
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

app.UseHttpsRedirection();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapControllers();

app.Run();
