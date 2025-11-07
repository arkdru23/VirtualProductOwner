using System.Security.Claims;
using BlazorApp1.Models;
using BlazorApp1.Services.Extraction;
using BlazorApp1.Services.Generator;
using BlazorApp1.Services.Llm;
using Microsoft.AspNetCore.Antiforgery;

namespace BlazorApp1.Endpoints;

public static class GenerationEndpoints
{
    public static IEndpointRouteBuilder MapGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        // /api/generate/with-context (multipart/form-data)
        app.MapPost("/api/generate/with-context", async (
            HttpContext ctx,
            IAntiforgery af,
            IContentExtractionService extractor,
            BlazorApp1.Services.Llm.ILlmClient llm,
            IStoryGeneratorService fallbackGenerator,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            if (!ctx.Request.HasFormContentType) return Results.BadRequest();

            var form = await ctx.Request.ReadFormAsync(ct);
            var input = form["input"].ToString();
            var files = form.Files.ToList();

            var extra = await extractor.ExtractTextAsync(files, ct);
            string Trunc(string s, int max) => s.Length <= max ? s : s[..max];
            var mergedContext = string.Join("\n\n", new[] { input, extra }.Where(s => !string.IsNullOrWhiteSpace(s)));
            mergedContext = Trunc(mergedContext, 16000);

            var stories = new List<Story>();
            var json = await llm.GenerateRawAsync(LlmPromptBuilder.Build(mergedContext), ct);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var arr = doc.RootElement.GetProperty("stories").EnumerateArray();
                    foreach (var el in arr)
                    {
                        var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        var desc = el.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        var pts = el.TryGetProperty("points", out var p) ? Math.Clamp(p.GetInt32(), 1, 13) : 3;
                        var ac = el.TryGetProperty("acceptanceCriteria", out var a) ? a.GetString() ?? "" : "";
                        
                        // Parse additional fields
                        var area = el.TryGetProperty("area", out var ar) ? ar.GetString() : null;
                        var iteration = el.TryGetProperty("iteration", out var it) ? it.GetString() : null;
                        var state = el.TryGetProperty("state", out var st) ? st.GetString() : "New";
                        var priority = el.TryGetProperty("priority", out var pr) && pr.TryGetInt32(out var prInt) ? prInt : (int?)null;
                        var risk = el.TryGetProperty("risk", out var ri) ? ri.GetString() : null;
                        var useCase = el.TryGetProperty("useCase", out var uc) ? uc.GetString() : null;

                        stories.Add(new Story
                        {
                            UserId = userId,
                            Title = title,
                            Description = desc,
                            Points = pts,
                            AcceptanceCriteria = ac,
                            Area = area,
                            Iteration = iteration,
                            State = state,
                            Priority = priority,
                            Risk = risk,
                            UseCase = useCase
                        });
                    }
                }
                catch { /* ignore and fallback */ }
            }

            if (stories.Count == 0)
            {
                stories = fallbackGenerator.Generate(userId, mergedContext).ToList();
            }

            return Results.Ok(stories);
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        // /api/generate/from-assets (POST JSON)
        app.MapPost("/api/generate/from-assets", async (
            HttpContext ctx,
            IAntiforgery af,
            BlazorApp1.Services.Context.IContextAssetService svc,
            BlazorApp1.Services.Llm.ILlmClient llm,
            IStoryGeneratorService fallbackGenerator,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var body = await ctx.Request.ReadFromJsonAsync<GenerateFromAssetsRequest>(cancellationToken: ct);
            if (body is null) return Results.BadRequest();

            var list = await svc.ListAsync(userId, ct);
            var selected = list.Where(a => body.AssetIds.Contains(a.Id)).ToList();

            var parts = new List<string> { body.Input ?? string.Empty };
            foreach (var a in selected)
            {
                var desc = string.IsNullOrWhiteSpace(a.TextExtract) ? $"[Asset: {a.FileName}] (no text extracted yet)" : $"[Asset: {a.FileName}]\n{a.TextExtract}";
                parts.Add(desc);
            }
            string Trunc(string s, int max) => s.Length <= max ? s : s[..max];
            var merged = Trunc(string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p))), 16000);

            var stories = new List<Story>();
            var json = await llm.GenerateRawAsync(LlmPromptBuilder.Build(merged), ct);

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var arr = doc.RootElement.GetProperty("stories").EnumerateArray();
                    foreach (var el in arr)
                    {
                        var title = el.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                        var desc = el.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                        var pts = el.TryGetProperty("points", out var p) ? Math.Clamp(p.GetInt32(), 1, 13) : 3;
                        var ac = el.TryGetProperty("acceptanceCriteria", out var a) ? a.GetString() ?? "" : "";
                        
                        // Parse additional fields
                        var area = el.TryGetProperty("area", out var ar) ? ar.GetString() : null;
                        var iteration = el.TryGetProperty("iteration", out var it) ? it.GetString() : null;
                        var state = el.TryGetProperty("state", out var st) ? st.GetString() : "New";
                        var priority = el.TryGetProperty("priority", out var pr) && pr.TryGetInt32(out var prInt) ? prInt : (int?)null;
                        var risk = el.TryGetProperty("risk", out var ri) ? ri.GetString() : null;
                        var useCase = el.TryGetProperty("useCase", out var uc) ? uc.GetString() : null;

                        stories.Add(new Story
                        {
                            UserId = userId,
                            Title = title,
                            Description = desc,
                            Points = pts,
                            AcceptanceCriteria = ac,
                            Area = area,
                            Iteration = iteration,
                            State = state,
                            Priority = priority,
                            Risk = risk,
                            UseCase = useCase
                        });
                    }
                }
                catch { /* ignore and fallback */ }
            }

            if (stories.Count == 0)
            {
                stories = fallbackGenerator.Generate(userId, merged).ToList();
            }

            return Results.Ok(stories);
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        // /api/generate/refine (POST JSON) - iteracyjne ulepszenie istniejącego story
        app.MapPost("/api/generate/refine", async (
            HttpContext ctx,
            IAntiforgery af,
            BlazorApp1.Services.Llm.ILlmClient llm,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var body = await ctx.Request.ReadFromJsonAsync<RefineStoryRequest>(cancellationToken: ct);
            if (body is null) return Results.BadRequest();

            // Zbuduj kontekst dla LLM
            var currentJson = LlmPromptBuilder.ToJson(new
            {
                title = body.Title,
                description = body.Description,
                points = body.Points,
                acceptanceCriteria = body.AcceptanceCriteria ?? string.Empty
            });

            var prompt = LlmPromptBuilder.BuildRefine(body.ExtraContext ?? string.Empty, currentJson);
            var json = await llm.GenerateRawAsync(prompt, ct);

            BlazorApp1.Models.RefinedStoryResponse result;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var title = root.TryGetProperty("title", out var t) ? t.GetString() ?? body.Title : body.Title;
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() ?? body.Description : body.Description;
                    var pts = root.TryGetProperty("points", out var p) ? Math.Clamp(p.GetInt32(), 1, 13) : body.Points;
                    var ac = root.TryGetProperty("acceptanceCriteria", out var a) ? a.GetString() ?? body.AcceptanceCriteria : body.AcceptanceCriteria;
                    
                    // Parse additional fields
                    var area = root.TryGetProperty("area", out var ar) ? ar.GetString() : null;
                    var priority = root.TryGetProperty("priority", out var pr) && pr.TryGetInt32(out var prInt) ? prInt : (int?)null;
                    var risk = root.TryGetProperty("risk", out var ri) ? ri.GetString() : null;
                    var useCase = root.TryGetProperty("useCase", out var uc) ? uc.GetString() : null;

                    result = new BlazorApp1.Models.RefinedStoryResponse
                    {
                        Title = title,
                        Description = desc,
                        Points = pts,
                        AcceptanceCriteria = ac,
                        Area = area,
                        Priority = priority,
                        Risk = risk,
                        UseCase = useCase
                    };
                }
                catch
                {
                    // w razie niepoprawnego JSON - fallback do wejściowych wartości
                    result = new BlazorApp1.Models.RefinedStoryResponse
                    {
                        Title = body.Title,
                        Description = body.Description,
                        Points = body.Points,
                        AcceptanceCriteria = body.AcceptanceCriteria
                    };
                }
            }
            else
            {
                // LLM wyłączony / brak klucza - zwróć oryginał
                result = new BlazorApp1.Models.RefinedStoryResponse
                {
                    Title = body.Title,
                    Description = body.Description,
                    Points = body.Points,
                    AcceptanceCriteria = body.AcceptanceCriteria
                };
            }

            return Results.Ok(result);
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        return app;
    }
}
