using BlazorApp1.Models;
using Microsoft.AspNetCore.Http;

namespace BlazorApp1.Services.Context;

public interface IContextAssetService
{
    Task<IReadOnlyList<ContextAsset>> ListAsync(string userId, CancellationToken ct = default);
    Task<ContextAsset> UploadAsync(string userId, IFormFile file, string webRootPath, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, Guid id, string webRootPath, CancellationToken ct = default);
    Task<int> ExtractAsync(string userId, IReadOnlyList<Guid> ids, string webRootPath, BlazorApp1.Services.Extraction.IContentExtractionService extractor, CancellationToken ct = default);
}
