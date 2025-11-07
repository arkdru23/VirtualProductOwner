using BlazorApp1.Models;
using BlazorApp1.Services.Stories;

namespace VirtualProductOwner.Tests.Services;

public class StoryServiceTests
{
    [Fact]
    public async Task CreateAndListShouldBeIsolatedPerUser()
    {
        // Arrange
        var sut = new StoryService();
        var userA = "user-A";
        var userB = "user-B";

        // Act
        var a1 = await sut.CreateAsync(userA, "A1", "Desc", 3);
        var a2 = await sut.CreateAsync(userA, "A2", "Desc", 5);
        var b1 = await sut.CreateAsync(userB, "B1", "Desc", 2);

        var listA = await sut.ListAsync(userA);
        var listB = await sut.ListAsync(userB);

        // Assert
        _ = listA.Should().HaveCount(2);
        _ = listB.Should().HaveCount(1);

        _ = listA.Select(s => s.Id).Should().Contain([a1.Id, a2.Id]);
        _ = listB.Select(s => s.Id).Should().Contain([b1.Id]);
    }

    [Fact]
    public async Task UpdateExistingStoryShouldPersistChangesAndReturnTrue()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-1";
        var created = await sut.CreateAsync(user, "Title", "Desc", 3);

        // Act
        var updated = new Story
        {
            Id = created.Id,
            UserId = user,
            Title = "Updated",
            Description = "Updated description",
            Points = 8,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };

        var ok = await sut.UpdateAsync(user, updated);
        var fetched = await sut.GetByIdAsync(user, created.Id);

        // Assert
        _ = ok.Should().BeTrue();
        _ = fetched.Should().NotBeNull();
        _ = fetched!.Title.Should().Be("Updated");
        _ = fetched.Description.Should().Be("Updated description");
        _ = fetched.Points.Should().Be(8);
        _ = fetched.UpdatedAt.Should().BeOnOrAfter(updated.UpdatedAt);
    }

    [Fact]
    public async Task UpdateNonExistingStoryShouldReturnFalse()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-2";
        var nonExisting = new Story
        {
            Id = Guid.NewGuid(),
            UserId = user,
            Title = "X",
            Description = "Y",
            Points = 3
        };

        // Act
        var ok = await sut.UpdateAsync(user, nonExisting);

        // Assert
        _ = ok.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteShouldRemoveStory()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-3";
        var s = await sut.CreateAsync(user, "To delete", "Desc", 3);

        // Act
        var removed = await sut.DeleteAsync(user, s.Id);
        var list = await sut.ListAsync(user);

        // Assert
        _ = removed.Should().BeTrue();
        _ = list.Should().NotContain(x => x.Id == s.Id);
    }

    [Fact]
    public async Task GetByIdShouldReturnStoryWhenExists()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-4";
        var s = await sut.CreateAsync(user, "Find me", "Desc", 5);

        // Act
        var fetched = await sut.GetByIdAsync(user, s.Id);

        // Assert
        _ = fetched.Should().NotBeNull();
        _ = fetched!.Id.Should().Be(s.Id);
        _ = fetched.Title.Should().Be("Find me");
        _ = fetched.Points.Should().Be(5);
    }

    [Fact]
    public async Task GetByIdShouldReturnNullWhenNotExists()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-5";

        // Act
        var fetched = await sut.GetByIdAsync(user, Guid.NewGuid());

        // Assert
        _ = fetched.Should().BeNull();
    }

    [Fact]
    public async Task Delete_NonExisting_ShouldReturnFalse()
    {
        // Arrange
        var sut = new StoryService();
        var user = "user-6";

        // Act
        var ok = await sut.DeleteAsync(user, Guid.NewGuid());

        // Assert
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task CrossUser_UpdateAndDelete_ShouldNotAffectOthers()
    {
        // Arrange
        var sut = new StoryService();
        var userA = "user-Ax";
        var userB = "user-Bx";
        var sA = await sut.CreateAsync(userA, "Owned by A", "Desc", 3);

        // Próba update po stronie B na Id z A
        var attemptUpdate = new Story
        {
            Id = sA.Id,
            UserId = userB,
            Title = "Hacked",
            Description = "Should not apply",
            Points = 8,
            CreatedAt = sA.CreatedAt,
            UpdatedAt = sA.UpdatedAt
        };

        // Act
        var updateOk = await sut.UpdateAsync(userB, attemptUpdate);
        var deleteOk = await sut.DeleteAsync(userB, sA.Id);
        var stillThere = await sut.GetByIdAsync(userA, sA.Id);

        // Assert
        updateOk.Should().BeFalse();
        deleteOk.Should().BeFalse();
        stillThere.Should().NotBeNull();
        stillThere!.Title.Should().Be("Owned by A");
        stillThere.UserId.Should().Be(userA);
    }
}
