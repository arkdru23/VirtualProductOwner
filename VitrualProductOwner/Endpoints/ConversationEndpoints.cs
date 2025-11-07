using System.Security.Claims;
using BlazorApp1.Data;
using BlazorApp1.Models;
using BlazorApp1.Services.Context;
using BlazorApp1.Services.Llm;
using BlazorApp1.Services.Stories;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stories/{id:guid}/conversation").RequireAuthorization();

        // GET history
        group.MapGet("/", async (Guid id, HttpContext ctx, StoryDbContext db, IStoryService stories, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var story = await stories.GetByIdAsync(userId, id, ct);
            if (story is null) return Results.NotFound(new { error = "story_not_found" });

            var msgs = await db.StoryMessages
                .Where(m => m.StoryId == id)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.Role, m.Content, m.CreatedAt })
                .ToListAsync(ct);

            return Results.Ok(new { messages = msgs });
        });

        // POST message
        group.MapPost("/messages", async (Guid id,
            HttpContext ctx,
            IAntiforgery af,
            StoryDbContext db,
            IStoryService stories,
            IServiceProvider sp,
            CancellationToken ct) =>
        {
            // Manual antiforgery validation - return 400 if missing/invalid
            try
            {
                await af.ValidateRequestAsync(ctx);
            }
            catch (AntiforgeryValidationException)
            {
                return Results.BadRequest(new { error = "csrf_validation_failed" });
            }

            try
            {
                ChatMessageRequest? body;
                try
                {
                    body = await ctx.Request.ReadFromJsonAsync<ChatMessageRequest>(cancellationToken: ct);
                    if (body is null)
                        return Results.BadRequest(new { error = "invalid_body" });
                }
                catch
                {
                    return Results.BadRequest(new { error = "invalid_body" });
                }

                var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                var story = await stories.GetByIdAsync(userId, id, ct);
                if (story is null) return Results.NotFound(new { error = "story_not_found" });

                // Ensure conversation row exists
                var conv = await db.StoryConversations.FirstOrDefaultAsync(c => c.StoryId == id, ct);
                if (conv is null)
                {
                    conv = new StoryConversation { StoryId = id };
                    db.StoryConversations.Add(conv);
                    await db.SaveChangesAsync(ct);
                }

                // Persist user message
                var userMsg = new StoryMessage
                {
                    StoryId = id, Role = "user", Content = body.Content ?? string.Empty, CreatedAt = DateTime.UtcNow
                };
                db.StoryMessages.Add(userMsg);
                await db.SaveChangesAsync(ct);

                // Build context
                var parts = new List<string>();
                parts.Add(
                    $"Current story:\n{{\"title\":\"{story.Title}\",\"description\":\"{story.Description}\",\"points\":{story.Points},\"acceptanceCriteria\":\"{story.AcceptanceCriteria ?? ""}\"}}");

                // Resolve optional services safely
                var assets = sp.GetService<IContextAssetService>();

                if (assets is not null && body.AssetIds is { Count: > 0 })
                {
                    var owned = await assets.ListAsync(userId, ct);
                    var selected = owned.Where(a => body.AssetIds.Contains(a.Id)).ToList();
                    foreach (var a in selected)
                    {
                        if (!string.IsNullOrWhiteSpace(a.TextExtract))
                        {
                            var snippet = a.TextExtract.Length > 4000 ? a.TextExtract[..4000] : a.TextExtract;
                            parts.Add($"[Asset: {a.FileName}]\n{snippet}");
                        }
                    }
                }

                // Reduce history: last 8 messages
                var history = await db.StoryMessages
                    .Where(m => m.StoryId == id)
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(8)
                    .OrderBy(m => m.CreatedAt)
                    .ToListAsync(ct);

                var extraContext = string.Join("\n\n", parts);

                // Compose refine prompt
                string currentJson;
                try
                {
                    currentJson = LlmPromptBuilder.ToJson(new
                    {
                        title = story.Title ?? "",
                        description = story.Description ?? "",
                        points = story.Points,
                        acceptanceCriteria = story.AcceptanceCriteria ?? ""
                    });
                }
                catch
                {
                    currentJson = "{}";
                }

                var merged = $"{extraContext}\n\nHistory:\n" +
                             string.Join("\n", history.Select(m => $"[{m.Role}] {m.Content}"));
                var prompt = LlmPromptBuilder.BuildRefine(merged, currentJson);

                // Call LLM (optional if not registered)
                var llm = sp.GetService<ILlmClient>();
                string? json = null;
                try
                {
                    json = llm is not null ? await llm.GenerateRawAsync(prompt, ct) : null;
                }
                catch
                {
                    // LLM call failed, continue with fallback
                    json = null;
                }
                RefinedStoryResponse suggestion;

                if (!string.IsNullOrWhiteSpace(json))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;
                        suggestion = new RefinedStoryResponse
                        {
                            Title =
                                root.TryGetProperty("title", out var t) ? t.GetString() ?? story.Title : story.Title,
                            Description = root.TryGetProperty("description", out var d)
                                ? d.GetString() ?? story.Description
                                : story.Description,
                            Points = root.TryGetProperty("points", out var p)
                                ? Math.Clamp(p.GetInt32(), 1, 13)
                                : story.Points,
                            AcceptanceCriteria = root.TryGetProperty("acceptanceCriteria", out var a)
                                ? a.GetString() ?? story.AcceptanceCriteria
                                : story.AcceptanceCriteria
                        };
                    }
                    catch
                    {
                        suggestion = new RefinedStoryResponse
                        {
                            Title = story.Title,
                            Description = story.Description,
                            Points = story.Points,
                            AcceptanceCriteria = story.AcceptanceCriteria
                        };
                    }
                }
                else
                {
                    suggestion = new RefinedStoryResponse
                    {
                        Title = story.Title,
                        Description = story.Description,
                        Points = story.Points,
                        AcceptanceCriteria = story.AcceptanceCriteria
                    };
                }

                // Save assistant message
                db.StoryMessages.Add(new StoryMessage
                {
                    StoryId = id,
                    Role = "assistant",
                    Content = $"Suggestion: {System.Text.Json.JsonSerializer.Serialize(suggestion)}",
                    CreatedAt = DateTime.UtcNow
                });
                conv.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                // Return history + suggestion
                var msgs = await db.StoryMessages
                    .Where(m => m.StoryId == id)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Role, m.Content, m.CreatedAt })
                    .ToListAsync(ct);

                return Results.Ok(new { messages = msgs, suggestion });
            }
            catch (Exception ex)
            {
                // Log full exception details for debugging
                Console.WriteLine($"ERROR in conversation handler: {ex}");
                return Results.StatusCode(500);
            }
        });

        return app;
    }


    public class ChatMessageRequest
    {
        public string? Content { get; set; }
        public List<Guid> AssetIds { get; set; } = new();
    }
}
