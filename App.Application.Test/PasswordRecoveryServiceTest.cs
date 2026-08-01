using App.Application.Interfaces;
using App.Application.Services;
using App.Application.Test.Fakers;
using App.Domain.Entities;
using App.Domain.Exceptions;
using NSubstitute;

namespace App.Application.Test;

public class PasswordRecoveryServiceTest
{
    private readonly IUserService _userService;
    private readonly IEmailSender _emailSender;
    private readonly IPasswordRecoveryService _passwordRecoveryService;

    public PasswordRecoveryServiceTest()
    {
        _userService = Substitute.For<IUserService>();
        _emailSender = Substitute.For<IEmailSender>();
        _passwordRecoveryService = new PasswordRecoveryService(_userService, _emailSender);
    }

    [Fact]
    public async Task SendResetTokenAsync_ShouldStoreTokenAndSendEmail_WhenUserExists()
    {
        // Arrange
        var user = UserFaker.GenerateUser();

        _userService.GetUserEntityByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _passwordRecoveryService.SendResetTokenAsync(user.Email);

        // Assert
        await _userService.Received(1).SetPasswordResetTokenAsync(user, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _emailSender.Received(1).SendPasswordResetEmailAsync(user.Email, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendResetTokenAsync_ShouldNotRevealEmailExistence_WhenUserDoesNotExist()
    {
        // Arrange
        var email = "notfound@email.com";

        _userService.GetUserEntityByEmailAsync(email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        await _passwordRecoveryService.SendResetTokenAsync(email);

        // Assert
        await _userService.DidNotReceive().SetPasswordResetTokenAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendPasswordResetEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldUpdatePassword_WhenTokenIsValid()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.SetPasswordResetToken("valid-token");
        var newPassword = "NewTest@123";

        _userService.GetByPasswordResetTokenAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        await _passwordRecoveryService.ResetPasswordAsync("valid-token", newPassword);

        // Assert
        await _userService.Received(1).ResetPasswordAsync(user, newPassword, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenTokenIsCleared()
    {
        // Arrange
        var user = UserFaker.GenerateUser();
        user.SetPasswordResetToken("cleared-token");
        user.ClearPasswordResetToken(); // token is now invalid

        _userService.GetByPasswordResetTokenAsync("cleared-token", Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var act = () => _passwordRecoveryService.ResetPasswordAsync("cleared-token", "NewTest@123");

        // Assert
        await Assert.ThrowsAsync<InvalidPasswordResetTokenException>(act);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldThrow_WhenTokenNotFound()
    {
        // Arrange
        _userService.GetByPasswordResetTokenAsync("invalid-token", Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var act = () => _passwordRecoveryService.ResetPasswordAsync("invalid-token", "NewTest@123");

        // Assert
        await Assert.ThrowsAsync<InvalidPasswordResetTokenException>(act);
    }
}
