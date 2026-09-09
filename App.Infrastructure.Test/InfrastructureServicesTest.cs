using App.Domain.Entities;
using App.Infrastructure.Data;
using App.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace App.Infrastructure.Test;

public sealed class InfrastructureServicesTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public InfrastructureServicesTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-with-at-least-32-characters!!",
                ["Jwt:Issuer"] = "OrkutNew.Tests",
                ["Jwt:Audience"] = "OrkutNew.Tests",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();
    }

    [Fact]
    public void BCryptPasswordHasher_ShouldHashAndVerifyPassword()
    {
        // Arrange
        var hasher = new BCryptPasswordHasher();

        // Act
        var hash = hasher.Hash("Password123!");

        // Assert
        Assert.NotEqual("Password123!", hash);
        Assert.True(hasher.Verify("Password123!", hash));
        Assert.False(hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void JwtTokenGenerator_ShouldIncludeExpectedClaims()
    {
        // Arrange
        var user = new User("Test User", "test@example.com", "test_user", "hash");
        var generator = new JwtTokenGenerator(_configuration, _context);

        // Act
        var token = generator.GenerateAccessToken(user);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        // Assert
        Assert.Equal(user.Id.ToString(), parsed.Subject);
        Assert.Equal(user.Email, parsed.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("OrkutNew.Tests", parsed.Issuer);
        Assert.Contains("OrkutNew.Tests", parsed.Audiences);
        Assert.True(parsed.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public async Task JwtTokenGenerator_ShouldPersistRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var generator = new JwtTokenGenerator(_configuration, _context);

        // Act
        var token = await generator.GenerateRefreshTokenAsync(userId);

        // Assert
        var stored = await _context.RefreshTokens.FindAsync(token.Id);
        Assert.NotNull(stored);
        Assert.Equal(userId, stored!.UserId);
        Assert.True(stored.IsValid());
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
