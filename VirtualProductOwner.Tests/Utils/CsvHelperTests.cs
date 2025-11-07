using BlazorApp1.Utils;

namespace VirtualProductOwner.Tests.Utils;

public class CsvHelperTests
{
    [Theory]
    [InlineData("Title,Description,3", new[] { "Title", "Description", "3" })]
    [InlineData("\"Ti,tle\",\"De\"\"sc\",5", new[] { "Ti,tle", "De\"sc", "5" })]
    [InlineData("\"A\",,\"7\"", new[] { "A", "", "7" })]
    public void ParseCsvLine_ShouldParseCorrectly(string line, string[] expected)
    {
        var parts = CsvHelper.ParseCsvLine(line).ToArray();
        parts.Should().Equal(expected);
    }

    [Fact]
    public void ParseCsvLine_ShouldHandleEmptyString()
    {
        var parts = CsvHelper.ParseCsvLine(string.Empty).ToArray();
        parts.Should().HaveCount(1);
        parts[0].Should().Be(string.Empty);
    }
}
