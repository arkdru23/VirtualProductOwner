using System.Text.RegularExpressions;
using BlazorApp1.Models;

namespace BlazorApp1.Services.Generator;

public partial class StoryGeneratorService : IStoryGeneratorService
{
    public IReadOnlyList<Story> Generate(string userId, string inputText)
    {
        var results = new List<Story>();
        if (string.IsNullOrWhiteSpace(inputText))
        {
            results.Add(new Story
            {
                UserId = userId,
                Title = "Generic story",
                Description = "As a user I want a default story so that I can see an example.",
                Points = 3
            });
            return results;
        }

        var lines = MyRegex().Split(inputText.Trim())
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        foreach (var line in lines)
        {
            var title = GenerateTitle(line);
            var desc = line;
            var points = EstimatePoints(line);
            results.Add(new Story
            {
                UserId = userId,
                Title = title,
                Description = desc,
                Points = points
            });
        }

        if (results.Count == 0)
        {
            results.Add(new Story
            {
                UserId = userId,
                Title = "Untitled story",
                Description = inputText.Trim(),
                Points = EstimatePoints(inputText)
            });
        }

        return results;
    }

    private static string GenerateTitle(string text)
    {
        // Use first 6-8 words as a compact title
        var words = Regex.Split(text, @"\W+").Where(w => !string.IsNullOrWhiteSpace(w)).Take(8).ToArray();
        return string.Join(' ', words);
    }

    private static int EstimatePoints(string text)
    {
        var words = Regex.Split(text, @"\W+").Count(w => !string.IsNullOrWhiteSpace(w));
        var estimate = Math.Max(1, Math.Min(13, (int)Math.Ceiling(words / 12.0)));
        // Slightly bump if contains strong keywords
        if (Regex.IsMatch(text, @"\b(must|required|critical)\b", RegexOptions.IgnoreCase))
        {
            estimate = Math.Min(13, estimate + 2);
        }
        else if (Regex.IsMatch(text, @"\b(should|important)\b", RegexOptions.IgnoreCase))
        {
            estimate = Math.Min(13, estimate + 1);
        }
        return estimate;
    }

    [GeneratedRegex(@"\r?\n")]
    private static partial Regex MyRegex();
}
