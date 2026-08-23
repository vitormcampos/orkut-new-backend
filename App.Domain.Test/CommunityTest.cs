using App.Domain.Entities;

namespace App.Domain.Test;

public class CommunityTest
{
    [Fact]
    public void Constructor_ShouldCreateCommunity_WhenDataIsValid()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        const string name = "Tech Community";

        // Act
        var community = new Community(ownerId, name, "A place for tech enthusiasts.");

        // Assert
        Assert.NotEqual(Guid.Empty, community.Id);
        Assert.Equal(ownerId, community.OwnerId);
        Assert.Equal(name, community.Name);
        Assert.Equal("A place for tech enthusiasts.", community.Description);
        Assert.Null(community.Photo);
        Assert.Null(community.UpdatedAt);
        Assert.True(community.CreatedAt <= DateTime.UtcNow);
        Assert.NotNull(community.Members);
        Assert.Empty(community.Members);
    }

    [Fact]
    public void Constructor_ShouldGenerateDifferentCommunities_OnEachCall()
    {
        // Arrange & Act
        var c1 = new Community(Guid.NewGuid(), "Community A", null);
        var c2 = new Community(Guid.NewGuid(), "Community B", null);

        // Assert
        Assert.NotEqual(c1.Id, c2.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var ownerId = Guid.NewGuid();

        // Act
        var act = () => new Community(ownerId, invalidName!, null);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOwnerIdIsEmpty()
    {
        // Arrange
        var emptyOwnerId = Guid.Empty;

        // Act
        var act = () => new Community(emptyOwnerId, "Tech Community", null);

        // Assert
        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("OwnerId", exception.Message);
    }

    [Fact]
    public void SetName_ShouldTrim_WhenValid()
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", null);

        // Act
        community.SetName("  Updated Community  ");

        // Assert
        Assert.Equal("Updated Community", community.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetName_ShouldThrow_WhenInvalid(string? invalidName)
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", null);

        // Act
        var act = () => community.SetName(invalidName!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void SetDescription_ShouldTrim_WhenValid()
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", null);

        // Act
        community.SetDescription("  A place for tech enthusiasts.  ");

        // Assert
        Assert.Equal("A place for tech enthusiasts.", community.Description);
    }

    [Fact]
    public void SetDescription_ShouldSetNull_WhenWhitespace()
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", "Existing description");

        // Act
        community.SetDescription("   ");

        // Assert
        Assert.Null(community.Description);
    }

    [Fact]
    public void SetPhoto_ShouldSetUrl()
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", null);

        // Act
        community.SetPhoto("https://example.com/photo.png");

        // Assert
        Assert.Equal("https://example.com/photo.png", community.Photo);
    }

    [Fact]
    public void Update_ShouldSetNameDescriptionAndUpdatedAt()
    {
        // Arrange
        var community = new Community(Guid.NewGuid(), "Tech Community", "Old description");

        // Act
        community.Update("New Name", "New description");

        // Assert
        Assert.Equal("New Name", community.Name);
        Assert.Equal("New description", community.Description);
        Assert.NotNull(community.UpdatedAt);
        Assert.True(community.UpdatedAt <= DateTime.UtcNow);
    }
}
