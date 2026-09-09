using App.Application.DTOs;
using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Test.Fakers;
using App.Domain.Entities;
using App.Domain.Exceptions;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace App.Application.Test;

public class AuthServiceTest : IDisposable
{
    private readonly AppDbContext _context;
    private readonly IUserService _userService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IAuthService _authService;

    public AuthServiceTest()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _userService = Substitute.For<IUserService>();
        _passwordHasher = Substitute.For<IPasswordHasher>();
        _tokenGenerator = Substitute.For<ITokenGenerator>();
        _authService = new AuthService(_context, _userService, _passwordHasher, _tokenGenerator);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_WhenCredentialsValid()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var request = new LoginRequest(user.Email, "Test@123");
        var accessToken = "access-token-abc";
        var refreshToken = new RefreshToken(user.Id, "refresh-token-xyz", DateTime.UtcNow.AddDays(7));

        _userService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(user.Id, user.Name, user.Email, user.Username, null, null, true, user.CreatedAt, null, null, null, null, null, null, null, null));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(true);
        _tokenGenerator.GenerateAccessToken(user).Returns(accessToken);
        _tokenGenerator.GenerateRefreshTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns(refreshToken);

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(refreshToken.Token, result.RefreshToken);
        Assert.Equal(user.Email, result.User.Email);
        // Verify LastLogin was called
        await _userService.Received(1).SetLastLoginAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest("notfound@email.com", "Test@123");

        _userService.GetByEmailAsync(request.Email, Arg.Any<CancellationToken>())
            .Returns((UserDto?)null);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(act);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenPasswordWrong()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var request = new LoginRequest(user.Email, "WrongPassword");

        _userService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(user.Id, user.Name, user.Email, user.Username, null, null, true, user.CreatedAt, null, null, null, null, null, null, null, null));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.Verify(request.Password, user.PasswordHash).Returns(false);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(act);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrow_WhenAccountDeactivated()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.Deactivate();
        var request = new LoginRequest(user.Email, "Test@123");

        _userService.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new UserDto(user.Id, user.Name, user.Email, user.Username, null, null, false, user.CreatedAt, null, null, null, null, null, null, null, null));

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        // Act
        var act = () => _authService.LoginAsync(request);

        // Assert
        await Assert.ThrowsAsync<AccountDeactivatedException>(act);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokenPair()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var oldRefreshToken = new RefreshToken(user.Id, "old-refresh", DateTime.UtcNow.AddDays(7));
        _context.Users.Add(user);
        _context.Set<RefreshToken>().Add(oldRefreshToken);
        await _context.SaveChangesAsync();

        var accessToken = "new-access-token";
        var newRefresh = new RefreshToken(user.Id, "new-refresh", DateTime.UtcNow.AddDays(7));

        _tokenGenerator.GenerateAccessToken(user).Returns(accessToken);
        _tokenGenerator.GenerateRefreshTokenAsync(user.Id, Arg.Any<CancellationToken>()).Returns(newRefresh);

        // Act
        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest(oldRefreshToken.Token));

        // Assert
        Assert.Equal(accessToken, result.AccessToken);
        Assert.Equal(newRefresh.Token, result.RefreshToken);

        // Old token revoked
        var revoked = await _context.Set<RefreshToken>().FindAsync(oldRefreshToken.Id);
        Assert.NotNull(revoked);
        Assert.True(revoked!.IsRevoked);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenTokenInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid-token");

        // Act
        var act = () => _authService.RefreshTokenAsync(request);

        // Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(act);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenTokenIsExpired()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var token = new RefreshToken(user.Id, "expired-refresh", DateTime.UtcNow.AddMinutes(-1));
        _context.Users.Add(user);
        _context.Set<RefreshToken>().Add(token);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.RefreshTokenAsync(new RefreshTokenRequest(token.Token));

        // Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(act);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrow_WhenUserIsDeactivated()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.Deactivate();
        var token = new RefreshToken(user.Id, "deactivated-refresh", DateTime.UtcNow.AddDays(1));
        _context.Users.Add(user);
        _context.Set<RefreshToken>().Add(token);
        await _context.SaveChangesAsync();

        // Act
        var act = () => _authService.RefreshTokenAsync(new RefreshTokenRequest(token.Token));

        // Assert
        await Assert.ThrowsAsync<AccountDeactivatedException>(act);
    }

    [Fact]
    public async Task LogoutAsync_ShouldIgnoreUnknownToken()
    {
        // Arrange

        // Act
        var exception = await Record.ExceptionAsync(() =>
            _authService.LogoutAsync("unknown-refresh"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task LogoutAsync_ShouldRevokeToken()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        var refreshToken = new RefreshToken(user.Id, "refresh-to-revoke", DateTime.UtcNow.AddDays(7));
        _context.Users.Add(user);
        _context.Set<RefreshToken>().Add(refreshToken);
        await _context.SaveChangesAsync();

        // Act
        await _authService.LogoutAsync(refreshToken.Token);

        // Assert
        var revoked = await _context.Set<RefreshToken>().FindAsync(refreshToken.Id);
        Assert.NotNull(revoked);
        Assert.True(revoked!.IsRevoked);
    }
}
