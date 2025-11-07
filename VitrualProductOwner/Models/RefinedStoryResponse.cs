namespace BlazorApp1.Models;

public class RefinedStoryResponse
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; } = 3;
    public string? AcceptanceCriteria { get; set; }
    public string? Area { get; set; }
    public int? Priority { get; set; }
    public string? Risk { get; set; }
    public string? UseCase { get; set; }
}
