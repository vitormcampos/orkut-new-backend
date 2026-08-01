using App.Application.Interfaces;
using App.Domain.Exceptions;

namespace App.Application.Services;

public class PasswordRecoveryService : IPasswordRecoveryService
{
    private readonly IUserService _userService;
    private readonly IEmailSender _emailSender;

    public PasswordRecoveryService(IUserService userService, IEmailSender emailSender)
    {
        _userService = userService;
        _emailSender = emailSender;
    }

    public async Task SendResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetUserEntityByEmailAsync(email.ToLowerInvariant(), cancellationToken);

        if (user is null)
            return; // Don't reveal whether email exists

        var token = Guid.NewGuid().ToString("N");

        await _userService.SetPasswordResetTokenAsync(user, token, cancellationToken);
        await _emailSender.SendPasswordResetEmailAsync(email, token, cancellationToken);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetByPasswordResetTokenAsync(token, cancellationToken);

        if (user is null || !user.IsPasswordResetTokenValid())
            throw new InvalidPasswordResetTokenException();

        await _userService.ResetPasswordAsync(user, newPassword, cancellationToken);
    }
}
