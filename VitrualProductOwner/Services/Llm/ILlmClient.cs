namespace BlazorApp1.Services.Llm;

public interface ILlmClient
{
    Task<string?> GenerateRawAsync(string prompt, CancellationToken ct = default);
}
