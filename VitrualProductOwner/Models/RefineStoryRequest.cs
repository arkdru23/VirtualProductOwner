namespace BlazorApp1.Models;

public class RefineStoryRequest
{
    public string? ExtraContext { get; set; }

    // Aktualne wartości story do ulepszenia
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; } = 3;
    public string? AcceptanceCriteria { get; set; }
}
