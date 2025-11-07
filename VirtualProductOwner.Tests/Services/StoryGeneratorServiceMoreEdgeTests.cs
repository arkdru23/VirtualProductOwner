using BlazorApp1.Services.Generator;

namespace VirtualProductOwner.Tests.Services;

public class StoryGeneratorServiceMoreEdgeTests
{
    private readonly StoryGeneratorService _sut = new();

    [Fact]
    public void GenerateShouldBeDeterministicForSameInput()
    {
        // Arrange
        var user = "det-user";
        var input = "As a user I want to search products so that I can find what I need quickly";

        // Act
        var first = _sut.Generate(user, input).Single();
        var second = _sut.Generate(user, input).Single();

        // Assert
        _ = second.UserId.Should().Be(first.UserId);
        _ = second.Title.Should().Be(first.Title);
        _ = second.Description.Should().Be(first.Description);
        _ = second.Points.Should().Be(first.Points);
    }

    [Fact]
    public void GenerateWithShouldKeywordShouldIncreasePointsByAtMostOneWithinBounds()
    {
        // Arrange
        var user = "u";
        var baseline = "As a user I want to export reports to csv to share with my team members easily";
        var withShould = "As a user I should export reports to csv to share with my team members easily";

        // Act
        var basePts = _sut.Generate(user, baseline).Single().Points;
        var shouldPts = _sut.Generate(user, withShould).Single().Points;

        // Assert
        _ = basePts.Should().BeInRange(1, 13);
        _ = shouldPts.Should().BeInRange(1, 13);
        // 'should' daje +1, ale nigdy ponad 13
        _ = shouldPts.Should().BeGreaterOrEqualTo(basePts);
        if (basePts < 13)
        {
            _ = shouldPts.Should().BeLessOrEqualTo(basePts + 1);
        }
    }

    [Fact]
    public void GenerateTitleShouldContainAtMostEightWords()
    {
        // Arrange
        var user = "u";
        var longSentence = "As a power user I want an extremely descriptive and very detailed title extraction mechanism for stories";

        // Act
        var story = _sut.Generate(user, longSentence).Single();

        // Assert
        _ = story.Title.Should().NotBeNullOrWhiteSpace();
        var words = story.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        _ = words.Length.Should().BeLessOrEqualTo(8);
    }

    [Fact]
    public void GenerateWhitespaceOnlyInputReturnsDefaultStory()
    {
        // Arrange
        var user = "u";
        var input = "   \t  \n  ";

        // Act
        var stories = _sut.Generate(user, input);

        // Assert
        _ = stories.Should().HaveCount(1);
        _ = stories[0].Title.Should().Be("Generic story");
        _ = stories[0].UserId.Should().Be(user);
    }
}
