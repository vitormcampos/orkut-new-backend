using App.Domain.Entities;
using App.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Test;

public sealed class AppDbContextInfrastructureTest : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AppDbContextInfrastructureTest()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task EnsureCreated_ShouldCreateConfiguredSchema()
    {
        var tableNames = await _context.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'")
            .ToListAsync();

        Assert.Contains("users", tableNames);
        Assert.Contains("friendships", tableNames);
        Assert.Contains("refresh_tokens", tableNames);
    }

    [Fact]
    public async Task Users_ShouldEnforceUniqueEmailAndUsername()
    {
        var first = new User("First", "same@example.com", "same_user", "hash");
        var duplicateEmail = new User("Second", "same@example.com", "second_user", "hash");
        var duplicateUsername = new User("Third", "third@example.com", "same_user", "hash");

        _context.Users.AddRange(first, duplicateEmail, duplicateUsername);

        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Friendship_ShouldPersistBothUserRelationships()
    {
        var requester = new User("Requester", "requester@example.com", "requester", "hash");
        var addressee = new User("Addressee", "addressee@example.com", "addressee", "hash");
        var friendship = new Friendship(requester.Id, addressee.Id);
        friendship.Accept();

        _context.AddRange(requester, addressee, friendship);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var stored = await _context.Friendships
            .Include(item => item.Requester)
            .Include(item => item.Addressee)
            .SingleAsync();

        Assert.Equal(FriendshipStatus.Accepted, stored.Status);
        Assert.Equal(requester.Username, stored.Requester.Username);
        Assert.Equal(addressee.Username, stored.Addressee.Username);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
