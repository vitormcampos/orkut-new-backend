using App.Application.DTOs;
using App.Application.Services;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Test;

public class PostServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly PostService _service;

    public PostServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new PostService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task CreateAsync_ShouldCreatePersonalPost_WhenCommunityIsNull()
    {
        // Arrange
        var author = CreateUser("Alice", "alice");
        _context.Users.Add(author);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateAsync(author.Id, new CreatePostRequest(null, "Meu post"));

        // Assert
        Assert.Equal(author.Id, result.AuthorId);
        Assert.Null(result.CommunityId);
        Assert.Equal("Meu post", result.Content);
        Assert.Single(await _context.Posts.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldReject_WhenAuthorIsNotCommunityMember()
    {
        // Arrange
        var owner = CreateUser("Alice", "alice");
        var outsider = CreateUser("Bob", "bob");
        _context.Users.AddRange(owner, outsider);
        await _context.SaveChangesAsync();
        var community = new Community(owner.Id, ".NET", "Developers");
        _context.Communities.Add(community);
        _context.CommunityMembers.Add(new CommunityMember(community.Id, owner.Id, MembershipRole.Owner));
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.CreateAsync(outsider.Id, new CreatePostRequest(community.Id, "Sem associação"));

        // Assert
        await Assert.ThrowsAsync<NotCommunityMemberException>(act);
        Assert.Empty(await _context.Posts.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowMemberToPostInCommunity()
    {
        // Arrange
        var owner = CreateUser("Alice", "alice");
        var member = CreateUser("Bob", "bob");
        _context.Users.AddRange(owner, member);
        await _context.SaveChangesAsync();
        var community = new Community(owner.Id, ".NET", "Developers");
        _context.Communities.Add(community);
        _context.CommunityMembers.AddRange(
            new CommunityMember(community.Id, owner.Id, MembershipRole.Owner),
            new CommunityMember(community.Id, member.Id, MembershipRole.Member));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CreateAsync(member.Id, new CreatePostRequest(community.Id, "Olá comunidade"));

        // Assert
        Assert.Equal(community.Id, result.CommunityId);
        Assert.Equal(member.Id, result.AuthorId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldAllowCommunityOwnerToModeratePost()
    {
        // Arrange
        var owner = CreateUser("Alice", "alice");
        var member = CreateUser("Bob", "bob");
        _context.Users.AddRange(owner, member);
        await _context.SaveChangesAsync();
        var community = new Community(owner.Id, ".NET", "Developers");
        _context.Communities.Add(community);
        _context.CommunityMembers.AddRange(
            new CommunityMember(community.Id, owner.Id, MembershipRole.Owner),
            new CommunityMember(community.Id, member.Id, MembershipRole.Member));
        var post = new Post(member.Id, community.Id, "Remover este post");
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeleteAsync(post.Id, owner.Id);

        // Assert
        Assert.Empty(await _context.Posts.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_ShouldReject_WhenUserIsNeitherAuthorNorCommunityOwner()
    {
        // Arrange
        var owner = CreateUser("Alice", "alice_denied");
        var author = CreateUser("Bob", "bob_denied");
        var outsider = CreateUser("Carol", "carol_denied");
        _context.Users.AddRange(owner, author, outsider);
        await _context.SaveChangesAsync();
        var community = new Community(owner.Id, "Community", null);
        _context.Communities.Add(community);
        _context.CommunityMembers.AddRange(
            new CommunityMember(community.Id, owner.Id, MembershipRole.Owner),
            new CommunityMember(community.Id, author.Id, MembershipRole.Member));
        var post = new Post(author.Id, community.Id, "Protected post");
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _service.DeleteAsync(post.Id, outsider.Id);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedPostActionException>(act);
        Assert.Single(await _context.Posts.ToListAsync());
    }

    [Fact]
    public async Task GetByUserAsync_ShouldReturnOnlyPersonalPosts()
    {
        // Arrange
        var user = CreateUser("Alice", "alice_posts");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        var community = new Community(user.Id, "Community", null);
        _context.Communities.Add(community);
        _context.CommunityMembers.Add(new CommunityMember(community.Id, user.Id, MembershipRole.Owner));
        _context.Posts.AddRange(
            new Post(user.Id, null, "Personal"),
            new Post(user.Id, community.Id, "Community post"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByUserAsync(user.Id);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Personal", Assert.Single(result.Items).Content);
    }

    private static User CreateUser(string name, string username) =>
        new(name, $"{username}@example.com", username, "hashed-password");
}
