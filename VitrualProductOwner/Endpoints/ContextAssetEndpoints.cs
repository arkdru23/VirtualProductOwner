using System.Security.Claims;
using BlazorApp1.Models;
using BlazorApp1.Services.Context;
using BlazorApp1.Services.Extraction;
using Microsoft.AspNetCore.Antiforgery;

namespace BlazorApp1.Endpoints;

public static class ContextAssetEndpoints
{
    public static IEndpointRouteBuilder MapContextAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/context").RequireAuthorization();

        assets.MapGet("/", async (IContextAssetService svc, HttpContext ctx, CancellationToken ct) =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var list = await svc.ListAsync(userId, ct);
            return Results.Ok(list);
        });

        assets.MapPost("/upload", async (HttpContext ctx, IAntiforgery af, IWebHostEnvironment env, IContextAssetService svc, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
            if (!ctx.Request.HasFormContentType) return Results.BadRequest();

            var form = await ctx.Request.ReadFormAsync(ct);
            var files = form.Files;
            var uploaded = new List<ContextAsset>();
            foreach (var f in files)
            {
                if (f.Length == 0) continue;
                var asset = await svc.UploadAsync(userId, f, env.WebRootPath, ct);
                uploaded.Add(asset);
            }
            return Results.Ok(uploaded);
        })
        .DisableAntiforgery();

        assets.MapPost("/extract", async (HttpContext ctx, IAntiforgery af, IWebHostEnvironment env, IContextAssetService svc, IContentExtractionService extractor, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var body = await ctx.Request.ReadFromJsonAsync<ExtractRequest>(cancellationToken: ct);
            if (body is null || body.AssetIds is null || body.AssetIds.Count == 0) return Results.BadRequest();

            var count = await svc.ExtractAsync(userId, body.AssetIds, env.WebRootPath, extractor, ct);
            return Results.Ok(new { extracted = count });
        })
        .DisableAntiforgery();

        assets.MapDelete("/{id:guid}", async (Guid id, HttpContext ctx, IAntiforgery af, IWebHostEnvironment env, IContextAssetService svc, CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var ok = await svc.DeleteAsync(userId, id, env.WebRootPath, ct);
            return ok ? Results.NoContent() : Results.NotFound();
        })
        .DisableAntiforgery();

        return app;
    }
}
