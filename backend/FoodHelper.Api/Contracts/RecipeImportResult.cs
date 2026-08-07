namespace FoodHelper.Api.Contracts;

public sealed class RecipeImportResult
{
    public string Name { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Id { get; init; }
    public string? Error { get; init; }
}
