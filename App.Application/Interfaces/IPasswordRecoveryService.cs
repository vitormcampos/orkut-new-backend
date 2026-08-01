namespace App.Application.Interfaces;

public interface IPasswordRecoveryService
{
    Task SendResetTokenAsync(string email, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
