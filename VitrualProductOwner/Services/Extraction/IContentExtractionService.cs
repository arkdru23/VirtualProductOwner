using Microsoft.AspNetCore.Http;

namespace BlazorApp1.Services.Extraction;

public interface IContentExtractionService
{
    Task<string> ExtractTextAsync(IReadOnlyList<IFormFile> files, CancellationToken ct = default);
}
