using BlazorApp1.Models;

namespace BlazorApp1.Services.Ado;

public interface IAdoService
{
    Task<(bool Success, string? WorkItemId, string? Url, string? Error)> CreateWorkItemAsync(
        Story story, 
        CancellationToken ct = default);
    
    Task<(bool Success, string? Error)> UpdateWorkItemAsync(
        string workItemId, 
        Story story, 
        CancellationToken ct = default);
    
    Task<bool> IsEnabledAsync();
}
