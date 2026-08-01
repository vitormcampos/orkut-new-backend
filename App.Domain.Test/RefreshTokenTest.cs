using App.Domain.Entities;

namespace App.Domain.Test;

public class RefreshTokenTest
{
    [Fact]
    public void Constructor_ShouldCreateRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "some-refresh-token-value";
        var expiresAt = DateTime.UtcNow.AddDays(7);

        // Act
        var refreshToken = new RefreshToken(userId, token, expiresAt);

        // Assert
        Assert.NotEqual(Guid.Empty, refreshToken.Id);
        Assert.Equal(userId, refreshToken.UserId);
        Assert.Equal(token, refreshToken.Token);
        Assert.Equal(expiresAt, refreshToken.ExpiresAt);
        Assert.False(refreshToken.IsRevoked);
        Assert.True(refreshToken.IsValid());
    }

    [Fact]
    public void Revoke_ShouldSetIsRevokedTrue()
    {
        // Arrange
        var token = new RefreshToken(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(7));

        // Act
        token.Revoke();

        // Assert
        Assert.True(token.IsRevoked);
        Assert.False(token.IsValid());
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenExpired()
    {
        // Arrange
        var token = new RefreshToken(Guid.NewGuid(), "token", DateTime.UtcNow.AddDays(-1));

        // Assert
        Assert.False(token.IsValid());
    }
}
