using System.Collections.Concurrent;
using BlazorApp1.Models;

namespace BlazorApp1.Services.Stories;

public class StoryService : IStoryService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Story>> _store = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<Story>> ListAsync(string userId, CancellationToken ct = default)
    {
        var userStories = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Story>());
        var list = userStories.Values.OrderBy(s => s.CreatedAt).ToList();
        return Task.FromResult<IReadOnlyList<Story>>(list);
    }

    public Task<Story> CreateAsync(string userId, string title, string description, int points, CancellationToken ct = default)
    {
        var userStories = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Story>());
        var story = new Story
        {
            UserId = userId,
            Title = title,
            Description = description,
            Points = points,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        userStories[story.Id] = story;
        return Task.FromResult(story);
    }

    public Task<bool> UpdateAsync(string userId, Story story, CancellationToken ct = default)
    {
        var userStories = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Story>());
        if (!userStories.ContainsKey(story.Id))
        {
            return Task.FromResult(false);
        }

        story.UserId = userId;
        story.UpdatedAt = DateTime.UtcNow;
        userStories[story.Id] = story;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var userStories = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Story>());
        var removed = userStories.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<Story?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default)
    {
        var userStories = _store.GetOrAdd(userId, _ => new ConcurrentDictionary<Guid, Story>());
        _ = userStories.TryGetValue(id, out var story);
        return Task.FromResult(story);
    }
}
