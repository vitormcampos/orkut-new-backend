using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Application.Test;

public class FriendshipServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IFriendshipService _friendshipService;

    public FriendshipServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _friendshipService = new FriendshipService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SendRequest_ShouldCreatePendingFriendship()
    {
        // Arrange
        var requester = CreateUser("Alice");
        var addressee = CreateUser("Bob");
        _context.Users.AddRange(requester, addressee);
        await _context.SaveChangesAsync();

        // Act
        var result = await _friendshipService.SendRequestAsync(requester.Id, "bob");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(requester.Id, result.RequesterId);
        Assert.Equal(addressee.Id, result.AddresseeId);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Alice", result.RequesterName);
        Assert.Equal("Bob", result.AddresseeName);
        Assert.Null(result.RespondedAt);
    }

    [Fact]
    public async Task SendRequest_ShouldThrow_WhenRequestToSelf()
    {
        // Arrange
        var user = CreateUser("Alice");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _friendshipService.SendRequestAsync(user.Id, "alice");

        // Assert
        await Assert.ThrowsAsync<FriendshipRequestToSelfException>(act);
    }

    [Fact]
    public async Task SendRequest_ShouldThrow_WhenAddresseeNotFound()
    {
        // Arrange
        var alice = CreateUser("Alice");
        _context.Users.Add(alice);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _friendshipService.SendRequestAsync(alice.Id, "nonexistent");

        // Assert
        await Assert.ThrowsAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task SendRequest_ShouldThrow_WhenAddresseeIsDeactivated()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        bob.Deactivate();
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Assert
        await Assert.ThrowsAsync<UserNotFoundException>(act);
    }

    [Fact]
    public async Task SendRequest_ShouldThrow_WhenAlreadyFriends()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var result = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(result.Id, bob.Id);

        // Act
        var act = () => _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Assert
        await Assert.ThrowsAsync<AlreadyFriendsException>(act);
    }

    [Fact]
    public async Task SendRequest_ShouldThrow_WhenAlreadyFriendsReversed()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var result = await _friendshipService.SendRequestAsync(bob.Id, "alice");
        await _friendshipService.AcceptRequestAsync(result.Id, alice.Id);

        // Act
        var act = () => _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Assert
        await Assert.ThrowsAsync<AlreadyFriendsException>(act);
    }

    [Fact]
    public async Task SendRequest_ShouldReturnExisting_WhenPendingAlreadyExists()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var first = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Act
        var second = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Pending", second.Status);
    }

    [Fact]
    public async Task SendRequest_ShouldResend_WhenPreviouslyRejected()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var first = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.RejectRequestAsync(first.Id, bob.Id);

        // Act
        var second = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Pending", second.Status);
        Assert.Null(second.RespondedAt);
    }

    [Fact]
    public async Task AcceptRequest_ShouldSetStatusAccepted()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Act
        var result = await _friendshipService.AcceptRequestAsync(request.Id, bob.Id);

        // Assert
        Assert.Equal("Accepted", result.Status);
        Assert.NotNull(result.RespondedAt);
    }

    [Fact]
    public async Task AcceptRequest_ShouldThrow_WhenNotAddressee()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Act
        var act = () => _friendshipService.AcceptRequestAsync(request.Id, alice.Id);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedFriendshipActionException>(act);
    }

    [Fact]
    public async Task AcceptRequest_ShouldThrow_WhenFriendshipNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _friendshipService.AcceptRequestAsync(nonExistentId, Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<FriendshipNotFoundException>(act);
    }

    [Fact]
    public async Task RejectRequest_ShouldSetStatusRejected()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Act
        var result = await _friendshipService.RejectRequestAsync(request.Id, bob.Id);

        // Assert
        Assert.Equal("Rejected", result.Status);
        Assert.NotNull(result.RespondedAt);
    }

    [Fact]
    public async Task RejectRequest_ShouldThrow_WhenNotAddressee()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");

        // Act
        var act = () => _friendshipService.RejectRequestAsync(request.Id, alice.Id);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedFriendshipActionException>(act);
    }

    [Fact]
    public async Task RemoveFriendship_ShouldDeleteRecord()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        _context.Users.AddRange(alice, bob);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(request.Id, bob.Id);

        // Act
        await _friendshipService.RemoveFriendshipAsync(request.Id, alice.Id);

        // Assert
        var deleted = await _context.Set<Friendship>().FindAsync(request.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task RemoveFriendship_ShouldThrow_WhenNotParticipant()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(alice, bob, charlie);
        await _context.SaveChangesAsync();

        var request = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(request.Id, bob.Id);

        // Act
        var act = () => _friendshipService.RemoveFriendshipAsync(request.Id, charlie.Id);

        // Assert
        await Assert.ThrowsAsync<NotFriendshipParticipantException>(act);
    }

    [Fact]
    public async Task RemoveFriendship_ShouldThrow_WhenFriendshipNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var act = () => _friendshipService.RemoveFriendshipAsync(nonExistentId, Guid.NewGuid());

        // Assert
        await Assert.ThrowsAsync<FriendshipNotFoundException>(act);
    }

    [Fact]
    public async Task GetFriends_ShouldReturnOnlyAccepted()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(alice, bob, charlie);
        await _context.SaveChangesAsync();

        var requestToBob = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(requestToBob.Id, bob.Id);

        await _friendshipService.SendRequestAsync(alice.Id, "charlie");

        // Act
        var friends = await _friendshipService.GetFriendsAsync(alice.Id);

        // Assert
        var list = friends.ToList();
        Assert.Single(list);
        Assert.Equal("Bob", list[0].AddresseeName);
    }

    [Fact]
    public async Task GetFriends_ShouldReturnEmpty_WhenNoFriends()
    {
        // Arrange
        var alice = CreateUser("Alice");
        _context.Users.Add(alice);
        await _context.SaveChangesAsync();

        // Act
        var friends = await _friendshipService.GetFriendsAsync(alice.Id);

        // Assert
        Assert.Empty(friends);
    }

    [Fact]
    public async Task GetPendingRequests_ShouldReturnOnlyPending()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(alice, bob, charlie);
        await _context.SaveChangesAsync();

        await _friendshipService.SendRequestAsync(bob.Id, "alice");
        await _friendshipService.SendRequestAsync(charlie.Id, "alice");

        var requestFromAlice = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(requestFromAlice.Id, bob.Id);

        // Act
        var pending = await _friendshipService.GetPendingRequestsAsync(alice.Id);

        // Assert
        var list = pending.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, f => Assert.Equal(alice.Id, f.AddresseeId));
        Assert.All(list, f => Assert.Equal("Pending", f.Status));
    }

    [Fact]
    public async Task GetSentRequests_ShouldReturnOnlyPendingSent()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        _context.Users.AddRange(alice, bob, charlie);
        await _context.SaveChangesAsync();

        await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.SendRequestAsync(alice.Id, "charlie");

        // Act
        var sent = await _friendshipService.GetSentRequestsAsync(alice.Id);

        // Assert
        var list = sent.ToList();
        Assert.Equal(2, list.Count);
        Assert.All(list, f => Assert.Equal(alice.Id, f.RequesterId));
        Assert.All(list, f => Assert.Equal("Pending", f.Status));
    }

    [Fact]
    public async Task GetFriendsInCommon_ShouldReturnMutualFriends()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        var dave = CreateUser("Dave");
        _context.Users.AddRange(alice, bob, charlie, dave);
        await _context.SaveChangesAsync();

        var ab = await _friendshipService.SendRequestAsync(alice.Id, "bob");
        await _friendshipService.AcceptRequestAsync(ab.Id, bob.Id);

        var ac = await _friendshipService.SendRequestAsync(alice.Id, "charlie");
        await _friendshipService.AcceptRequestAsync(ac.Id, charlie.Id);

        var bc = await _friendshipService.SendRequestAsync(bob.Id, "charlie");
        await _friendshipService.AcceptRequestAsync(bc.Id, charlie.Id);

        var bd = await _friendshipService.SendRequestAsync(bob.Id, "dave");
        await _friendshipService.AcceptRequestAsync(bd.Id, dave.Id);

        // Act
        var common = await _friendshipService.GetFriendsInCommonAsync(alice.Id, bob.Id);

        // Assert
        var list = common.ToList();
        Assert.Single(list);
        Assert.Equal("Charlie", list[0].Name);
    }

    [Fact]
    public async Task GetFriendsInCommon_ShouldReturnEmpty_WhenNoMutualFriends()
    {
        // Arrange
        var alice = CreateUser("Alice");
        var bob = CreateUser("Bob");
        var charlie = CreateUser("Charlie");
        var dave = CreateUser("Dave");
        _context.Users.AddRange(alice, bob, charlie, dave);
        await _context.SaveChangesAsync();

        var ac = await _friendshipService.SendRequestAsync(alice.Id, "charlie");
        await _friendshipService.AcceptRequestAsync(ac.Id, charlie.Id);

        var bd = await _friendshipService.SendRequestAsync(bob.Id, "dave");
        await _friendshipService.AcceptRequestAsync(bd.Id, dave.Id);

        // Act
        var common = await _friendshipService.GetFriendsInCommonAsync(alice.Id, bob.Id);

        // Assert
        Assert.Empty(common);
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
