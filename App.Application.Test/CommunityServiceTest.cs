using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Test.Fakers;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Test;

public class CommunityServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly ICommunityService _communityService;

    public CommunityServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _communityService = new CommunityService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDto_WithMemberCountOneAndOwnerAsMember()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var request = CommunityFaker.GenerateCreateRequest();

        // Act
        var result = await _communityService.CreateAsync(owner.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(owner.Id, result.OwnerId);
        Assert.Equal("Alice", result.OwnerName);
        Assert.Equal(1, result.MemberCount);
        Assert.Equal(request.Name, result.Name);

        var members = await _context.Set<CommunityMember>().ToListAsync();
        Assert.Single(members);
        Assert.Equal(result.Id, members[0].CommunityId);
        Assert.Equal(owner.Id, members[0].UserId);
        Assert.Equal(MembershipRole.Owner, members[0].Role);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldThrow_WhenNameIsInvalid(string? invalidName)
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var request = new CreateCommunityRequest(invalidName!, "Description");

        // Act
        var act = () => _communityService.CreateAsync(ownerId, request);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdate_WhenOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());
        var request = new UpdateCommunityRequest("New Name", "New Description");

        // Act
        var result = await _communityService.UpdateAsync(community.Id, owner.Id, request);

        // Assert
        Assert.Equal("New Name", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNotOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var other = CreateUser("Bob");
        _context.Users.AddRange(owner, other);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var act = () => _communityService.UpdateAsync(community.Id, other.Id, CommunityFaker.GenerateUpdateRequest());

        // Assert
        await Assert.ThrowsAsync<UnauthorizedCommunityActionException>(act);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _communityService.UpdateAsync(nonExistentId, Guid.NewGuid(), CommunityFaker.GenerateUpdateRequest());

        // Assert
        await Assert.ThrowsAsync<CommunityNotFoundException>(act);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCommunityExists()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var created = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var result = await _communityService.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(created.Id, result!.Id);
        Assert.Equal("Alice", result.OwnerName);
        Assert.Equal(1, result.MemberCount);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _communityService.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnCommunitiesTheUserBelongsTo()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var member = CreateUser("Bob");
        _context.Users.AddRange(owner, member);
        await _context.SaveChangesAsync();

        var joined = await _communityService.CreateAsync(
            owner.Id,
            new CreateCommunityRequest("Joined", "Joined community"));
        await _communityService.JoinAsync(joined.Id, member.Id);

        // Act
        var result = await _communityService.GetByUserIdAsync(member.Id);

        // Assert
        var community = Assert.Single(result);
        Assert.Equal(joined.Id, community.Id);
        Assert.Equal("Joined", community.Name);
        Assert.Equal(2, community.MemberCount);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmpty_WhenUserHasNoCommunities()
    {
        // Arrange
        var user = CreateUser("Alice");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _communityService.GetByUserIdAsync(user.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldThrow_WhenUserDoesNotExist()
    {
        // Act
        var act = () => _communityService.GetByUserIdAsync(Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task SearchAsync_ShouldMatchNameOrDescription_CaseInsensitively_AndCalculateMemberCount()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var member = CreateUser("Bob");
        _context.Users.AddRange(owner, member);
        await _context.SaveChangesAsync();
        var community = await _communityService.CreateAsync(owner.Id, new CreateCommunityRequest("Games", "The best PC games"));
        await _communityService.JoinAsync(community.Id, member.Id);
        await _communityService.CreateAsync(owner.Id, new CreateCommunityRequest("Cooking", "Recipes"));

        // Act
        var result = await _communityService.SearchAsync("  PC GAMES  ");

        // Assert
        var match = Assert.Single(result);
        Assert.Equal(community.Id, match.Id);
        Assert.Equal(2, match.MemberCount);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmpty_WhenThereAreNoMatches()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();
        await _communityService.CreateAsync(owner.Id, new CreateCommunityRequest("Games", "Description"));

        // Act
        var result = await _communityService.SearchAsync("unknown");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAsync_ShouldThrowValidationException_WhenTermIsTooLong()
    {
        // Arrange
        var term = new string('x', 101);

        // Act
        var act = () => _communityService.SearchAsync(term);

        // Assert
        await Assert.ThrowsAsync<App.Application.Exceptions.ValidationException>(act);
    }

    [Fact]
    public async Task JoinAsync_ShouldCreateMember()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(owner, bob);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        await _communityService.JoinAsync(community.Id, bob.Id);

        // Assert
        var membership = await _context.Set<CommunityMember>()
            .FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.UserId == bob.Id);
        Assert.NotNull(membership);
        Assert.Equal(MembershipRole.Member, membership!.Role);
    }

    [Fact]
    public async Task JoinAsync_ShouldThrow_WhenAlreadyMember()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(owner, bob);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());
        await _communityService.JoinAsync(community.Id, bob.Id);

        // Act
        var act = () => _communityService.JoinAsync(community.Id, bob.Id);

        // Assert
        await Assert.ThrowsAsync<AlreadyCommunityMemberException>(act);
    }

    [Fact]
    public async Task JoinAsync_ShouldThrow_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _communityService.JoinAsync(nonExistentId, Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<CommunityNotFoundException>(act);
    }

    [Fact]
    public async Task LeaveAsync_ShouldRemoveMember()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(owner, bob);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());
        await _communityService.JoinAsync(community.Id, bob.Id);

        // Act
        await _communityService.LeaveAsync(community.Id, bob.Id);

        // Assert
        var membership = await _context.Set<CommunityMember>()
            .FirstOrDefaultAsync(m => m.CommunityId == community.Id && m.UserId == bob.Id);
        Assert.Null(membership);
    }

    [Fact]
    public async Task LeaveAsync_ShouldThrow_WhenNotMember()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(owner, bob);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var act = () => _communityService.LeaveAsync(community.Id, bob.Id);

        // Assert
        await Assert.ThrowsAsync<NotCommunityMemberException>(act);
    }

    [Fact]
    public async Task LeaveAsync_ShouldThrow_WhenOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var act = () => _communityService.LeaveAsync(community.Id, owner.Id);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedCommunityActionException>(act);
    }

    [Fact]
    public async Task LeaveAsync_ShouldThrow_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _communityService.LeaveAsync(nonExistentId, Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<CommunityNotFoundException>(act);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldReturnMembersOrderedByJoinedAt()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(owner, bob, charlie);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());
        await _communityService.JoinAsync(community.Id, bob.Id);
        await Task.Delay(5);
        await _communityService.JoinAsync(community.Id, charlie.Id);

        // Act
        var members = await _communityService.GetMembersAsync(community.Id);

        // Assert
        var list = members.ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal(owner.Id, list[0].UserId);
        Assert.Equal("Alice", list[0].UserName);
        Assert.Equal(bob.Id, list[1].UserId);
        Assert.Equal(charlie.Id, list[2].UserId);
        Assert.Equal("Member", list[1].Role);
    }

    [Fact]
    public async Task GetMembersAsync_ShouldThrow_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _communityService.GetMembersAsync(nonExistentId);

        // Assert
        await Assert.ThrowsAsync<CommunityNotFoundException>(act);
    }

    [Fact]
    public async Task SetPhotoAsync_ShouldSetPhoto_WhenOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var result = await _communityService.SetPhotoAsync(community.Id, owner.Id, "https://example.com/photo.png");

        // Assert
        Assert.Equal("https://example.com/photo.png", result.Photo);
    }

    [Fact]
    public async Task SetPhotoAsync_ShouldThrow_WhenNotOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var other = CreateUser("Bob");
        _context.Users.AddRange(owner, other);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var act = () => _communityService.SetPhotoAsync(community.Id, other.Id, "https://example.com/photo.png");

        // Assert
        await Assert.ThrowsAsync<UnauthorizedCommunityActionException>(act);
    }

    [Fact]
    public async Task SetPhotoAsync_ShouldThrow_WhenCommunityNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _communityService.SetPhotoAsync(nonExistentId, Guid.NewGuid(), "https://example.com/photo.png");

        // Assert
        await Assert.ThrowsAsync<CommunityNotFoundException>(act);
    }

    [Fact]
    public async Task GetMemberCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var owner = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(owner, bob, charlie);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());
        await _communityService.JoinAsync(community.Id, bob.Id);
        await _communityService.JoinAsync(community.Id, charlie.Id);

        // Act
        var count = await _communityService.GetMemberCountAsync(community.Id);

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task GetMemberCountAsync_ShouldReturnOne_WhenOnlyOwner()
    {
        // Arrange
        var owner = CreateUser("Alice");
        _context.Users.Add(owner);
        await _context.SaveChangesAsync();

        var community = await _communityService.CreateAsync(owner.Id, CommunityFaker.GenerateCreateRequest());

        // Act
        var count = await _communityService.GetMemberCountAsync(community.Id);

        // Assert
        Assert.Equal(1, count);
    }

    private static User CreateUser(string name)
    {
        return new User(
            name,
            $"{name.ToLower()}@test.com",
            name.ToLower(),
            "Test@123"
        );
    }
}
