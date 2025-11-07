using BlazorApp1.Services.Generator;

namespace VirtualProductOwner.Tests.Services;

public class StoryGeneratorServiceEdgeTests
{
    private readonly StoryGeneratorService _sut = new();

    [Fact]
    public void GenerateShouldIgnoreEmptyAndWhitespaceOnlyLines()
    {
        // Arrange
        var user = "u";
        var input = """
                    line one



                    line two
                    """;

        // Act
        var stories = _sut.Generate(user, input);

        // Assert
        _ = stories.Should().HaveCount(2);
        _ = stories.All(s => s.UserId == user).Should().BeTrue();
    }

    [Fact]
    public void GenerateVeryLongLineShouldRespectPointsUpperBound()
    {
        // Arrange
        var user = "u";
        var longLine = string.Join(' ', Enumerable.Repeat("word", 500)); // bardzo długa linia (>> 13 pkt bazowo)

        // Act
        var stories = _sut.Generate(user, longLine);

        // Assert
        _ = stories.Should().HaveCount(1);
        _ = stories[0].Points.Should().BeInRange(1, 13);
        _ = stories[0].Points.Should().Be(13); // sufit punktacji
    }

    [Fact]
    public void GenerateWithCriticalKeywordAndLongLineShouldCapAt13()
    {
        // Arrange
        var user = "u";
        var text = "This is a critical requirement " + string.Join(' ', Enumerable.Repeat("word", 200));

        // Act
        var points = _sut.Generate(user, text).Single().Points;

        // Assert
        _ = points.Should().Be(13); // bazowo wysoko +2 za 'critical', ale ograniczone do 13
    }

    [Fact]
    public void GenerateShouldTrimTitleAndDescriptionInputs()
    {
        // Arrange
        var user = "u";
        var input = "   As a user I want trimmed line so that it is clean    ";

        // Act
        var story = _sut.Generate(user, input).Single();

        // Assert
        _ = story.Title.Should().NotBeNullOrWhiteSpace();
        _ = story.Description.Should().Be(input.Trim());
    }
}
