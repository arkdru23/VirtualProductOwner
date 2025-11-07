using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BlazorApp1.Models;
using Microsoft.Extensions.Options;

namespace BlazorApp1.Services.Ado;

public class AdoService : IAdoService
{
    private readonly HttpClient _httpClient;
    private readonly AdoOptions _options;
    private readonly ILogger<AdoService> _logger;

    public AdoService(HttpClient httpClient, IOptions<AdoOptions> options, ILogger<AdoService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<bool> IsEnabledAsync()
    {
        return Task.FromResult(_options.Enabled && 
                               !string.IsNullOrWhiteSpace(_options.Organization) &&
                               !string.IsNullOrWhiteSpace(_options.Project) &&
                               !string.IsNullOrWhiteSpace(_options.PersonalAccessToken));
    }

    public async Task<(bool Success, string? WorkItemId, string? Url, string? Error)> CreateWorkItemAsync(
        Story story, 
        CancellationToken ct = default)
    {
        if (!await IsEnabledAsync())
        {
            return (false, null, null, "Azure DevOps integration is not enabled or not configured");
        }

        try
        {
            var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/wit/workitems/${_options.WorkItemType}?api-version=7.0";

            var operations = new List<object>
            {
                new { op = "add", path = "/fields/System.Title", value = story.Title },
                new { op = "add", path = "/fields/System.Description", value = story.Description },
                new { op = "add", path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints", value = story.Points }
            };

            // Add optional fields
            if (!string.IsNullOrWhiteSpace(story.AcceptanceCriteria))
                operations.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = story.AcceptanceCriteria });

            if (!string.IsNullOrWhiteSpace(story.Area))
                operations.Add(new { op = "add", path = "/fields/System.AreaPath", value = $"{_options.Project}\\{story.Area}" });
            else if (!string.IsNullOrWhiteSpace(_options.DefaultAreaPath))
                operations.Add(new { op = "add", path = "/fields/System.AreaPath", value = _options.DefaultAreaPath });

            if (!string.IsNullOrWhiteSpace(story.Iteration))
                operations.Add(new { op = "add", path = "/fields/System.IterationPath", value = $"{_options.Project}\\{story.Iteration}" });
            else if (!string.IsNullOrWhiteSpace(_options.DefaultIteration))
                operations.Add(new { op = "add", path = "/fields/System.IterationPath", value = _options.DefaultIteration });

            if (story.Priority.HasValue)
                operations.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Common.Priority", value = story.Priority.Value });

            if (!string.IsNullOrWhiteSpace(story.Risk))
                operations.Add(new { op = "add", path = "/fields/Microsoft.VSTS.Common.Risk", value = story.Risk });

            if (!string.IsNullOrWhiteSpace(story.State))
                operations.Add(new { op = "add", path = "/fields/System.State", value = story.State });

            var json = JsonSerializer.Serialize(operations);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}")));
            request.Content = content;

            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Failed to create ADO work item: {StatusCode} - {Error}", response.StatusCode, error);
                return (false, null, null, $"ADO API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var id = doc.RootElement.GetProperty("id").GetInt32().ToString();
            var workItemUrl = doc.RootElement.GetProperty("_links").GetProperty("html").GetProperty("href").GetString();

            _logger.LogInformation("Created ADO work item {WorkItemId} for story {StoryId}", id, story.Id);
            return (true, id, workItemUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating ADO work item for story {StoryId}", story.Id);
            return (false, null, null, ex.Message);
        }
    }

    public async Task<(bool Success, string? Error)> UpdateWorkItemAsync(
        string workItemId, 
        Story story, 
        CancellationToken ct = default)
    {
        if (!await IsEnabledAsync())
        {
            return (false, "Azure DevOps integration is not enabled or not configured");
        }

        try
        {
            var url = $"https://dev.azure.com/{_options.Organization}/{_options.Project}/_apis/wit/workitems/{workItemId}?api-version=7.0";

            var operations = new List<object>
            {
                new { op = "replace", path = "/fields/System.Title", value = story.Title },
                new { op = "replace", path = "/fields/System.Description", value = story.Description },
                new { op = "replace", path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints", value = story.Points }
            };

            if (!string.IsNullOrWhiteSpace(story.AcceptanceCriteria))
                operations.Add(new { op = "replace", path = "/fields/Microsoft.VSTS.Common.AcceptanceCriteria", value = story.AcceptanceCriteria });

            if (story.Priority.HasValue)
                operations.Add(new { op = "replace", path = "/fields/Microsoft.VSTS.Common.Priority", value = story.Priority.Value });

            if (!string.IsNullOrWhiteSpace(story.State))
                operations.Add(new { op = "replace", path = "/fields/System.State", value = story.State });

            var json = JsonSerializer.Serialize(operations);
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            using var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", 
                Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_options.PersonalAccessToken}")));
            request.Content = content;

            var response = await _httpClient.SendAsync(request, ct);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Failed to update ADO work item {WorkItemId}: {StatusCode} - {Error}", workItemId, response.StatusCode, error);
                return (false, $"ADO API error: {response.StatusCode}");
            }

            _logger.LogInformation("Updated ADO work item {WorkItemId} for story {StoryId}", workItemId, story.Id);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception updating ADO work item {WorkItemId} for story {StoryId}", workItemId, story.Id);
            return (false, ex.Message);
        }
    }
}
