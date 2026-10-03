using App.Domain.Entities;

namespace App.Domain.Test;

public class PostTest
{
    [Fact]
    public void Constructor_ShouldCreatePersonalPost_WhenCommunityIsNull()
    {
        // Arrange
        var authorId = Guid.NewGuid();

        // Act
        var post = new Post(authorId, null, "  Olá, Orkut!  ");

        // Assert
        Assert.NotEqual(Guid.Empty, post.Id);
        Assert.Equal(authorId, post.AuthorId);
        Assert.Null(post.CommunityId);
        Assert.Equal("Olá, Orkut!", post.Content);
        Assert.True(post.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_ShouldCreateCommunityPost_WhenCommunityIsProvided()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var communityId = Guid.NewGuid();

        // Act
        var post = new Post(authorId, communityId, "Post da comunidade");

        // Assert
        Assert.Equal(authorId, post.AuthorId);
        Assert.Equal(communityId, post.CommunityId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAuthorIdIsEmpty()
    {
        // Arrange
        var communityId = Guid.NewGuid();

        // Act
        var act = () => new Post(Guid.Empty, communityId, "Conteúdo válido");

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenContentIsEmpty(string? content)
    {
        // Arrange
        var authorId = Guid.NewGuid();

        // Act
        var act = () => new Post(authorId, null, content!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentExceedsMaximumLength()
    {
        // Arrange
        var authorId = Guid.NewGuid();

        // Act
        var act = () => new Post(authorId, null, new string('a', Post.MaxContentLength + 1));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }
}
