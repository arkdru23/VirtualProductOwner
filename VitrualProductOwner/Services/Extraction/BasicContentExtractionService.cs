using Microsoft.AspNetCore.Http;

namespace BlazorApp1.Services.Extraction;

public class BasicContentExtractionService : IContentExtractionService
{
    public async Task<string> ExtractTextAsync(IReadOnlyList<IFormFile> files, CancellationToken ct = default)
    {
        if (files.Count == 0) return string.Empty;

        var parts = new List<string>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is ".txt" or ".md")
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms, ct);
                ms.Position = 0;
                using var reader = new StreamReader(ms);
                var text = await reader.ReadToEndAsync();
                parts.Add($"[File: {file.FileName}]\n{text}");
            }
            else if (ext is ".pdf")
            {
                parts.Add($"[PDF: {file.FileName}] PDF text extraction not enabled in this build. Configure OCR/PDF extractor to include content.");
            }
            else if (ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".svg")
            {
                parts.Add($"[Image: {file.FileName}] Image OCR not enabled in this build. Configure Vision OCR to include content.");
            }
            else
            {
                parts.Add($"[Attachment: {file.FileName}] Unsupported file type for text extraction.");
            }
        }

        return string.Join("\n\n", parts);
    }
}
