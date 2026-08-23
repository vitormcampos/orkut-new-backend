using App.Application.Interfaces;
using App.Application.Services;
using App.Domain.Entities;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace App.Application.Test;

public class ProfileServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IFriendshipService _friendshipService;
    private readonly IProfileService _profileService;

    public ProfileServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _friendshipService = Substitute.For<IFriendshipService>();
        _profileService = new ProfileService(_context, _friendshipService);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetPublicProfileAsync_ShouldReturnProfile_WithFriendCountAndAge()
    {
        // Arrange
        var user = new User("Alice", "alice@test.com", "alice", "Test@123");
        user.SetBirthDate(new DateOnly(1990, 5, 15));
        user.SetLocation("São Paulo", "SP");
        user.SetRelationshipStatus(RelationshipStatus.Married);
        user.SetInterests(new[] { "Rock" }, new[] { "Action" }, new[] { "Fiction" }, new[] { "Reading" });
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _friendshipService.GetFriendCountAsync(user.Id, Arg.Any<CancellationToken>()).Returns(42);

        // Act
        var result = await _profileService.GetPublicProfileAsync("alice");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result!.Id);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("alice", result.Username);
        Assert.Equal("São Paulo", result.City);
        Assert.Equal("SP", result.State);
        Assert.Equal("Married", result.RelationshipStatus);
        Assert.Equal(new[] { "Rock" }, result.MusicInterests);
        Assert.Equal(new[] { "Action" }, result.MovieInterests);
        Assert.Equal(new[] { "Fiction" }, result.BookInterests);
        Assert.Equal(new[] { "Reading" }, result.Hobbies);
        Assert.Equal(42, result.FriendCount);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedAge = today.Year - 1990;
        if (new DateOnly(1990, 5, 15) > today.AddYears(-expectedAge)) expectedAge--;
        Assert.Equal(expectedAge, result.Age);

        await _friendshipService.Received(1).GetFriendCountAsync(user.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPublicProfileAsync_ShouldReturnNull_WhenUsernameNotFound()
    {
        // Arrange

        // Act
        var result = await _profileService.GetPublicProfileAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicProfileAsync_ShouldReturnNull_WhenUserInactive()
    {
        // Arrange
        var user = new User("Alice", "alice@test.com", "alice", "Test@123");
        user.Deactivate();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.GetPublicProfileAsync("alice");

        // Assert
        Assert.Null(result);
    }
}
