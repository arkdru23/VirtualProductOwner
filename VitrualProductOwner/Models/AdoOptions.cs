namespace BlazorApp1.Models;

public class AdoOptions
{
    public bool Enabled { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string WorkItemType { get; set; } = "User Story";
    public string DefaultAreaPath { get; set; } = string.Empty;
    public string DefaultIteration { get; set; } = string.Empty;
}
