using System.Security.Claims;
using BlazorApp1.Models;
using BlazorApp1.Services.Stories;
using BlazorApp1.Utils;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

namespace BlazorApp1.Endpoints;

public static class StoriesEndpoints
{
    public static IEndpointRouteBuilder MapStoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var stories = app.MapGroup("/api/stories").RequireAuthorization();

        stories.MapGet("/", async (IStoryService service, HttpContext ctx, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            var list = await service.ListAsync(userId, ct);
            return Results.Ok(list);
        });

        stories.MapPost("/", async ([FromBody] CreateStoryRequest body, IStoryService service, HttpContext ctx, IAntiforgery af, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var created = await service.CreateAsync(userId, body.Title.Trim(), body.Description.Trim(), body.Points, ct);
            return Results.Created($"/api/stories/{created.Id}", created);
        })
        .DisableAntiforgery();

        stories.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateStoryRequest body, IStoryService service, HttpContext ctx, IAntiforgery af, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            if (id != body.Id) return Results.BadRequest();

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var updated = new Story
            {
                Id = id,
                UserId = userId,
                Title = body.Title.Trim(),
                Description = body.Description.Trim(),
                Points = body.Points,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var ok = await service.UpdateAsync(userId, updated, ct);
            return ok ? Results.Ok() : Results.NotFound();
        })
        .DisableAntiforgery();

        stories.MapDelete("/{id:guid}", async (Guid id, IStoryService service, HttpContext ctx, IAntiforgery af, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var ok = await service.DeleteAsync(userId, id, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .DisableAntiforgery();

        stories.MapGet("/export", async (IStoryService service, HttpContext ctx, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var list = await service.ListAsync(userId, ct);
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Title,Description,Points,Area,Iteration,State,AssignedTo,Priority,Risk,TargetDate,AcceptanceCriteria,RelatedWorkItem,UseCase");
            foreach (var s in list)
            {
                static string Csv(string? v) => $"\"{(v ?? string.Empty).Replace("\"", "\"\"")}\"";
                var target = s.TargetDate?.ToString("yyyy-MM-dd") ?? "";
                var priority = s.Priority?.ToString() ?? "";
                sb.AppendLine($"{Csv(s.Title)},{Csv(s.Description)},{s.Points},{Csv(s.Area)},{Csv(s.Iteration)},{Csv(s.State)},{Csv(s.AssignedTo)},{priority},{Csv(s.Risk)},{Csv(target)},{Csv(s.AcceptanceCriteria)},{Csv(s.RelatedWorkItem)},{Csv(s.UseCase)}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"stories-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return Results.File(bytes, "text/csv", fileName);
        });

        stories.MapPost("/import", async (HttpContext ctx, IStoryService service, IAntiforgery af, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            if (!ctx.Request.HasFormContentType) return Results.BadRequest();
            var form = await ctx.Request.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0) return Results.BadRequest();

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            string? line;
            int imported = 0;
            bool headerSkipped = false;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped && line.StartsWith("Title", StringComparison.OrdinalIgnoreCase)) { headerSkipped = true; continue; }
                headerSkipped = true;

                var parts = CsvHelper.ParseCsvLine(line).ToArray();
                if (parts.Length < 3) continue;

                var title = parts[0].Trim();
                var desc = parts[1].Trim();
                var pts = int.TryParse(parts[2], out var p) ? p : 3;
                if (string.IsNullOrWhiteSpace(title)) continue;

                var created = await service.CreateAsync(userId, title, desc, Math.Clamp(pts, 1, 13), ct);

                string? Get(int idx) => idx < parts.Length ? parts[idx].Trim().Trim('"') : null;
                created.Area = Get(3);
                created.Iteration = Get(4);
                created.State = Get(5);
                created.AssignedTo = Get(6);
                created.Priority = int.TryParse(Get(7), out var ip) ? ip : null;
                created.Risk = Get(8);
                created.TargetDate = DateTime.TryParse(Get(9), out var dt) ? dt : null;
                created.AcceptanceCriteria = Get(10);
                created.RelatedWorkItem = Get(11);
                created.UseCase = Get(12);

                await service.UpdateAsync(userId, created, ct);

                imported++;
            }

            return Results.Ok(new { imported });
        })
        .DisableAntiforgery();

        return app;
    }
}
