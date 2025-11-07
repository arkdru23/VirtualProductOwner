using BlazorApp1.Services.Llm;

namespace VirtualProductOwner.Tests.Services.Llm;

public class LlmPromptBuilderTests
{
    [Fact]
    public void Build_ShouldContainCoreSectionsAndRules()
    {
        var context = "Some user and system context text.";
        var s = LlmPromptBuilder.Build(context);

        // Core sections
        s.Should().Contain("Product Owner assistant");
        s.Should().Contain("Context:");
        s.Should().Contain(context);
        s.Should().Contain("Generate detailed user stories in JSON format");
        s.Should().Contain("\"stories\"");
        s.Should().Contain("Rules:");

        // Schema fields
        s.Should().Contain("\"title\"");
        s.Should().Contain("\"description\"");
        s.Should().Contain("\"points\"");
        s.Should().Contain("\"acceptanceCriteria\"");
        s.Should().Contain("\"area\"");
        s.Should().Contain("\"priority\"");
        s.Should().Contain("\"risk\"");
        s.Should().Contain("\"useCase\"");

        // Rules
        s.Should().Contain("1-13");
        s.Should().Contain("Fibonacci");
        s.Should().Contain("Given-When-Then");
    }

    [Fact]
    public void BuildRefine_ShouldReferenceStoryJsonAndExtraContext()
    {
        var extra = "Please make title shorter.";
        var storyJson = "{\"title\":\"Long title\",\"description\":\"Desc\",\"points\":8,\"acceptanceCriteria\":\"A;B\"}";
        var s = LlmPromptBuilder.BuildRefine(extra, storyJson);

        // Core sections
        s.Should().Contain("Product Owner assistant");
        s.Should().Contain("Refine the provided user story");
        s.Should().Contain("Feedback:");
        s.Should().Contain(extra);
        s.Should().Contain("Current story JSON:");
        s.Should().Contain(storyJson);
        s.Should().Contain("Return a single JSON object");

        // Additional fields
        s.Should().Contain("\"area\"");
        s.Should().Contain("\"priority\"");
        s.Should().Contain("\"risk\"");
        s.Should().Contain("\"useCase\"");

        // Rules
        s.Should().Contain("1-13");
        s.Should().Contain("Fibonacci");
    }
}
