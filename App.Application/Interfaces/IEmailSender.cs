namespace App.Application.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default);
}
