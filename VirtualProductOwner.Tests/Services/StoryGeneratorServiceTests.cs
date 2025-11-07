using BlazorApp1.Services.Generator;

namespace VirtualProductOwner.Tests.Services;

public class StoryGeneratorServiceTests
{
    private readonly StoryGeneratorService _sut = new();

    [Fact]
    public void GenerateWhenInputIsEmptyReturnsDefaultStory()
    {
        // Arrange
        var userId = "user-1";
        var input = "";

        // Act
        var stories = _sut.Generate(userId, input);

        // Assert
        _ = stories.Should().NotBeNull();
        _ = stories.Should().HaveCount(1);
        var s = stories[0];
        _ = s.UserId.Should().Be(userId);
        _ = s.Title.Should().Be("Generic story");
        _ = s.Description.Should().NotBeNullOrWhiteSpace();
        _ = s.Points.Should().BeInRange(1, 13);
    }

    [Fact]
    public void GenerateWithMultipleLinesReturnsStoryPerLineWithDescriptionsAndTitles()
    {
        // Arrange
        var userId = "user-2";
        var input = """
                    As a user I want to log in so that I can access my dashboard
                    As an admin I want to manage users so that I can maintain the system
                    """;

        // Act
        var stories = _sut.Generate(userId, input);

        // Assert
        _ = stories.Should().HaveCount(2);
        _ = stories.All(s => s.UserId == userId).Should().BeTrue();
        _ = stories.All(s => !string.IsNullOrWhiteSpace(s.Title)).Should().BeTrue();
        _ = stories.All(s => !string.IsNullOrWhiteSpace(s.Description)).Should().BeTrue();
        _ = stories.All(s => s.Points is >= 1 and <= 13).Should().BeTrue();
    }

    [Fact]
    public void GenerateWithPriorityKeywordsIncreasesEstimatedPoints()
    {
        // Arrange
        var userId = "user-3";

        // Two lines of similar length; the one with "must" should yield higher points deterministically
        var baseline = "As a user I want to view my profile details and update info";
        var withMust = "As a user I must view my profile details and update info";

        // Act
        var basePoints = _sut.Generate(userId, baseline).Single().Points;
        var mustPoints = _sut.Generate(userId, withMust).Single().Points;

        // Assert
        _ = mustPoints.Should().BeGreaterThan(basePoints);
        _ = mustPoints.Should().BeInRange(1, 13);
        _ = basePoints.Should().BeInRange(1, 13);
    }
}
