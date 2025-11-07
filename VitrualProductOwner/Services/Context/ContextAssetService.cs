using BlazorApp1.Data;
using BlazorApp1.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Services.Context;

public class ContextAssetService : IContextAssetService
{
    private readonly StoryDbContext _db;

    public ContextAssetService(StoryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ContextAsset>> ListAsync(string userId, CancellationToken ct = default)
    {
        return await _db.ContextAssets
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ContextAsset> UploadAsync(string userId, IFormFile file, string webRootPath, CancellationToken ct = default)
    {
        var userFolder = Path.Combine(webRootPath, "uploads", userId);
        Directory.CreateDirectory(userFolder);

        var safeName = Path.GetFileName(file.FileName);
        var id = Guid.NewGuid();
        var storedName = $"{id}_{safeName}";
        var fullPath = Path.Combine(userFolder, storedName);

        await using (var fs = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(fs, ct);
        }

        var relPath = Path.Combine("uploads", userId, storedName).Replace('\\', '/');

        var asset = new ContextAsset
        {
            Id = id,
            UserId = userId,
            FileName = safeName,
            ContentType = file.ContentType ?? "application/octet-stream",
            Size = file.Length,
            StoragePath = relPath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ContextAssets.Add(asset);
        await _db.SaveChangesAsync(ct);

        return asset;
    }

    public async Task<bool> DeleteAsync(string userId, Guid id, string webRootPath, CancellationToken ct = default)
    {
        var asset = await _db.ContextAssets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (asset is null) return false;

        _db.ContextAssets.Remove(asset);
        await _db.SaveChangesAsync(ct);

        var fullPath = Path.Combine(webRootPath, asset.StoragePath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(fullPath))
        {
            try { File.Delete(fullPath); } catch { /* ignore */ }
        }
        return true;
    }

    public async Task<int> ExtractAsync(string userId, IReadOnlyList<Guid> ids, string webRootPath, BlazorApp1.Services.Extraction.IContentExtractionService extractor, CancellationToken ct = default)
    {
        var target = await _db.ContextAssets
            .Where(a => a.UserId == userId && ids.Contains(a.Id))
            .ToListAsync(ct);

        int count = 0;
        foreach (var a in target)
        {
            var fullPath = Path.Combine(webRootPath, a.StoragePath.Replace('/', Path.DirectorySeparatorChar));
            var files = new List<IFormFile>();

            // Create a FormFile-like wrapper to reuse extractor
            var stream = File.OpenRead(fullPath);
            var formFile = new FormFile(stream, 0, stream.Length, "file", a.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = a.ContentType
            };
            files.Add(formFile);

            var text = await extractor.ExtractTextAsync(files, ct);
            a.TextExtract = text;
            a.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            // Dispose stream
            stream.Dispose();
            count++;
        }
        return count;
    }
}
