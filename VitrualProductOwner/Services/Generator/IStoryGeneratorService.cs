using BlazorApp1.Models;

namespace BlazorApp1.Services.Generator;

public interface IStoryGeneratorService
{
    IReadOnlyList<Story> Generate(string userId, string inputText);
}
