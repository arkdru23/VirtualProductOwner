using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp1.Services.Llm;

public class HttpLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _options;

    public HttpLlmClient(HttpClient http, IOptions<LlmOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task<string?> GenerateRawAsync(string prompt, CancellationToken ct = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        // Resolve endpoint URI (fallback to OpenAI default if not provided or invalid)
        var endpoint = (_options.Endpoint ?? string.Empty).Trim();
        Uri uri;
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out uri))
        {
            uri = new Uri("https://api.openai.com/v1/chat/completions");
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, uri);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Authorization differs for Azure OpenAI vs OpenAI; here support both by header switch
        if (string.Equals(_options.Provider, "AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            req.Headers.TryAddWithoutValidation("api-key", _options.ApiKey);
        }
        else
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        }

        var body = new
        {
            model = _options.Model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = "You are a Product Owner assistant. Generate user stories in JSON with fields: title, description, points, acceptanceCriteria. Points: 1..13. Output JSON only." },
                new { role = "user", content = prompt }
            }
        };

        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        // OpenAI format: choices[0].message.content
        try
        {
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return content;
        }
        catch
        {
            return null;
        }
    }
}
