using App.Application.DTOs;
using App.Application.Services;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Test;

public class ScrapServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ScrapService _service;

    public ScrapServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new ScrapService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Create_ShouldCreateScrapWithAuthorAndRecipient()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateAsync(
            author.Id,
            new CreateScrapRequest(recipient.Id, "Fala, Bob!"));

        // Assert
        Assert.Equal(author.Id, result.AuthorId);
        Assert.Equal(recipient.Id, result.RecipientId);
        Assert.Equal("Alice", result.AuthorName);
        Assert.Equal("Public", result.Visibility);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldReturnPublicScrapsToAnonymousViewer()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        _context.Scraps.Add(new Scrap(author.Id, recipient.Id, "Público"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProfileScrapsAsync(recipient.Id, null);

        // Assert
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldHidePrivateScrapFromAnonymousViewer()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        _context.Scraps.Add(new Scrap(author.Id, recipient.Id, "Privado", ScrapVisibility.Private));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProfileScrapsAsync(recipient.Id, null);

        // Assert
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldReturnNotFoundForInactiveProfile()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        recipient.Deactivate();
        _context.Users.AddRange(author, recipient);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.GetProfileScrapsAsync(recipient.Id, null);

        // Assert
        await Assert.ThrowsAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldPaginateNewestFirst()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        _context.Scraps.AddRange(
            new Scrap(author.Id, recipient.Id, "Primeiro"),
            new Scrap(author.Id, recipient.Id, "Segundo"),
            new Scrap(author.Id, recipient.Id, "Terceiro"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProfileScrapsAsync(recipient.Id, null, 2, 2);

        // Assert
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalItems);
        Assert.False(result.HasNextPage);
        var scrap = Assert.Single(result.Items);
        Assert.Equal("Primeiro", scrap.Content);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldClampPageSizeToFifty()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProfileScrapsAsync(recipient.Id, null, 1, 500);

        // Assert
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task Create_ShouldThrow_WhenVisibilityIsInvalid()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(
            author.Id,
            new CreateScrapRequest(recipient.Id, "Oi", "Internal"));

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldHidePrivateScrapFromOtherViewer()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        var otherViewer = CreateUser("Carol", "carol");
        _context.Users.AddRange(author, recipient, otherViewer);
        _context.Scraps.Add(new Scrap(author.Id, recipient.Id, "Privado", ScrapVisibility.Private));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetProfileScrapsAsync(recipient.Id, otherViewer.Id);

        // Assert
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetProfileScraps_ShouldShowPrivateScrapToAuthorAndRecipient()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        _context.Scraps.Add(new Scrap(author.Id, recipient.Id, "Privado", ScrapVisibility.Private));
        await _context.SaveChangesAsync();

        // Act
        var authorView = await _service.GetProfileScrapsAsync(recipient.Id, author.Id);
        var recipientView = await _service.GetProfileScrapsAsync(recipient.Id, recipient.Id);

        // Assert
        Assert.Single(authorView.Items);
        Assert.Single(recipientView.Items);
    }

    [Fact]
    public async Task Delete_ShouldThrow_WhenCurrentUserIsNotAuthor()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        var recipient = CreateUser("Bob", "bob");
        _context.Users.AddRange(author, recipient);
        var scrap = new Scrap(author.Id, recipient.Id, "Oi");
        _context.Scraps.Add(scrap);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.DeleteAsync(scrap.Id, recipient.Id);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedScrapActionException>(act);
    }

    private static User CreateUser(string name, string username)
    {
        return new User(name, $"{username}@example.com", username, "hash");
    }
}
