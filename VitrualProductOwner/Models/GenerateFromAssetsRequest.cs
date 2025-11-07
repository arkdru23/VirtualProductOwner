namespace BlazorApp1.Models;

public record GenerateFromAssetsRequest(string? Input, List<Guid> AssetIds);
