using System.Security.Claims;
using BlazorApp1.Models;
using BlazorApp1.Services.Ado;
using BlazorApp1.Services.Stories;
using Microsoft.AspNetCore.Antiforgery;

namespace BlazorApp1.Endpoints;

public static class ApprovalEndpoints
{
    public static IEndpointRouteBuilder MapApprovalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stories/{id:guid}/approval").RequireAuthorization();

        // Submit for approval
        group.MapPost("/submit", async (
            Guid id,
            HttpContext ctx,
            IAntiforgery af,
            IStoryService stories,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var story = await stories.GetByIdAsync(userId, id, ct);
            if (story is null) return Results.NotFound();

            if (story.ApprovalStatus != ApprovalStatus.Draft)
                return Results.BadRequest(new { error = "Story must be in Draft status to submit for approval" });

            story.ApprovalStatus = ApprovalStatus.PendingApproval;
            story.UpdatedAt = DateTime.UtcNow;

            var ok = await stories.UpdateAsync(userId, story, ct);
            return ok ? Results.Ok(new { status = "pending_approval" }) : Results.Problem();
        })
        .DisableAntiforgery();

        // Approve story
        group.MapPost("/approve", async (
            Guid id,
            HttpContext ctx,
            IAntiforgery af,
            IStoryService stories,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            // TODO: Check if user has approver role
            // For now, any authenticated user can approve

            var story = await stories.GetByIdAsync(userId, id, ct);
            if (story is null) return Results.NotFound();

            if (story.ApprovalStatus != ApprovalStatus.PendingApproval)
                return Results.BadRequest(new { error = "Story must be in Pending Approval status to approve" });

            story.ApprovalStatus = ApprovalStatus.Approved;
            story.ApprovedBy = userId;
            story.ApprovedAt = DateTime.UtcNow;
            story.UpdatedAt = DateTime.UtcNow;

            var ok = await stories.UpdateAsync(userId, story, ct);
            return ok ? Results.Ok(new { status = "approved", approvedBy = userId, approvedAt = story.ApprovedAt }) : Results.Problem();
        })
        .DisableAntiforgery();

        // Reject story
        group.MapPost("/reject", async (
            Guid id,
            HttpContext ctx,
            IAntiforgery af,
            IStoryService stories,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var body = await ctx.Request.ReadFromJsonAsync<RejectRequest>(cancellationToken: ct);
            
            var story = await stories.GetByIdAsync(userId, id, ct);
            if (story is null) return Results.NotFound();

            if (story.ApprovalStatus != ApprovalStatus.PendingApproval)
                return Results.BadRequest(new { error = "Story must be in Pending Approval status to reject" });

            story.ApprovalStatus = ApprovalStatus.Rejected;
            story.RejectionReason = body?.Reason;
            story.UpdatedAt = DateTime.UtcNow;

            var ok = await stories.UpdateAsync(userId, story, ct);
            return ok ? Results.Ok(new { status = "rejected", reason = story.RejectionReason }) : Results.Problem();
        })
        .DisableAntiforgery();

        // Sync to Azure DevOps
        group.MapPost("/sync-to-ado", async (
            Guid id,
            HttpContext ctx,
            IAntiforgery af,
            IStoryService stories,
            IAdoService ado,
            CancellationToken ct) =>
        {
            try { await af.ValidateRequestAsync(ctx); } catch (AntiforgeryValidationException) { return Results.BadRequest(); }

            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var story = await stories.GetByIdAsync(userId, id, ct);
            if (story is null) return Results.NotFound();

            if (story.ApprovalStatus != ApprovalStatus.Approved)
                return Results.BadRequest(new { error = "Only approved stories can be synced to Azure DevOps" });

            if (!await ado.IsEnabledAsync())
                return Results.BadRequest(new { error = "Azure DevOps integration is not enabled" });

            // Create or update work item
            if (string.IsNullOrEmpty(story.AdoWorkItemId))
            {
                // Create new work item
                var (success, workItemId, url, error) = await ado.CreateWorkItemAsync(story, ct);
                if (!success)
                    return Results.Problem(error ?? "Failed to create ADO work item");

                story.AdoWorkItemId = workItemId;
                story.AdoWorkItemUrl = url;
                story.SyncedToAdoAt = DateTime.UtcNow;
                story.UpdatedAt = DateTime.UtcNow;

                await stories.UpdateAsync(userId, story, ct);

                return Results.Ok(new 
                { 
                    status = "created", 
                    workItemId, 
                    url,
                    syncedAt = story.SyncedToAdoAt 
                });
            }
            else
            {
                // Update existing work item
                var (success, error) = await ado.UpdateWorkItemAsync(story.AdoWorkItemId, story, ct);
                if (!success)
                    return Results.Problem(error ?? "Failed to update ADO work item");

                story.SyncedToAdoAt = DateTime.UtcNow;
                story.UpdatedAt = DateTime.UtcNow;

                await stories.UpdateAsync(userId, story, ct);

                return Results.Ok(new 
                { 
                    status = "updated", 
                    workItemId = story.AdoWorkItemId, 
                    url = story.AdoWorkItemUrl,
                    syncedAt = story.SyncedToAdoAt 
                });
            }
        })
        .DisableAntiforgery();

        return app;
    }

    private record RejectRequest(string? Reason);
}
