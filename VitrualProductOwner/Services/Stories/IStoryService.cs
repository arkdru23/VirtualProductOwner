using BlazorApp1.Models;

namespace BlazorApp1.Services.Stories;

public interface IStoryService
{
    Task<IReadOnlyList<Story>> ListAsync(string userId, CancellationToken ct = default);
    Task<Story> CreateAsync(string userId, string title, string description, int points, CancellationToken ct = default);
    Task<bool> UpdateAsync(string userId, Story story, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, Guid id, CancellationToken ct = default);
    Task<Story?> GetByIdAsync(string userId, Guid id, CancellationToken ct = default);
}
