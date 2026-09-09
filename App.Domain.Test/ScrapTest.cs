using App.Domain.Entities;

namespace App.Domain.Test;

public class ScrapTest
{
    [Fact]
    public void Constructor_ShouldCreatePublicScrapByDefault()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        // Act
        var scrap = new Scrap(authorId, recipientId, "  Oi, sumido!  ");

        // Assert
        Assert.NotEqual(Guid.Empty, scrap.Id);
        Assert.Equal("Oi, sumido!", scrap.Content);
        Assert.Equal(ScrapVisibility.Public, scrap.Visibility);
        Assert.Equal(authorId, scrap.AuthorId);
        Assert.Equal(recipientId, scrap.RecipientId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAuthorIsRecipient()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = () => new Scrap(userId, userId, "Oi");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentIsInvalid()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        // Act
        var empty = () => new Scrap(authorId, recipientId, " ");
        var tooLong = () => new Scrap(authorId, recipientId, new string('a', Scrap.MaxContentLength + 1));

        // Assert
        Assert.Throws<ArgumentException>(empty);
        Assert.Throws<ArgumentException>(tooLong);
    }
}
