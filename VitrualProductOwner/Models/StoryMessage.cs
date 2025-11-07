namespace BlazorApp1.Models;

public class StoryMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StoryId { get; set; }
    public string Role { get; set; } = "user"; // "user" | "assistant" | "system"
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
