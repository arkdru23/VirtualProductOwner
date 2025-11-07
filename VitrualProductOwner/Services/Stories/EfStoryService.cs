using BlazorApp1.Data;
using BlazorApp1.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Services.Stories;

public class EfStoryService : IStoryService
{
    private readonly StoryDbContext _db;

    public EfStoryService(StoryDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Story>> ListAsync(string userId, CancellationToken ct = default)
    {
        var list = await _db.Stories
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return list;
    }

    public async Task<Story> CreateAsync(string userId, string title, string description, int points, CancellationToken ct = default)
    {
        var story = new Story
        {
            UserId = userId,
            Title = title,
            Description = description,
            Points = points,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Stories.Add(story);
        await _db.SaveChangesAsync(ct);
        return story;
    }

    public async Task<bool> UpdateAsync(string userId, Story story, CancellationToken ct = default)
    {
        var existing = await _db.Stories
            .Where(s => s.UserId == userId && s.Id == story.Id)
            .SingleOrDefaultAsync(ct);

        if (existing is null)
            return false;

        existing.Title = story.Title;
        existing.Description = story.Description;
        existing.Points = story.Points;

        existing.Area = story.Area;
        existing.Iteration = story.Iteration;
        existing.State = story.State;
        existing.AssignedTo = story.AssignedTo;
        existing.Priority = story.Priority;
        existing.Risk = story.Risk;
        existing.TargetDate = story.TargetDate;
        existing.AcceptanceCriteria = story.AcceptanceCriteria;
        existing.RelatedWorkItem = story.RelatedWorkItem;
        existing.UseCase = story.UseCase;

        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var existing = await _db.Stories
            .Where(s => s.UserId == userId && s.Id == id)
            .SingleOrDefaultAsync(ct);

        if (existing is null)
            return false;

        _db.Stories.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<Story?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default)
    {
        return _db.Stories
            .Where(s => s.UserId == userId && s.Id == id)
            .SingleOrDefaultAsync(ct);
    }
}
