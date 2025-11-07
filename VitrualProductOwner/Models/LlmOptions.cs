namespace BlazorApp1.Models;

public class LlmOptions
{
    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "OpenAI"; // or "AzureOpenAI"
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}
