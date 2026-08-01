using System.Net;
using System.Net.Mail;
using App.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendPasswordResetEmailAsync(string email, string resetToken, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"]!;
        var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
        var username = _configuration["Smtp:Username"]!;
        var password = _configuration["Smtp:Password"]!;
        var fromEmail = _configuration["Smtp:FromEmail"]!;
        var resetUrl = $"{_configuration["App:BaseUrl"]}/reset-password?token={resetToken}";

        using var client = new SmtpClient(host, port);
        client.EnableSsl = true;
        client.Credentials = new NetworkCredential(username, password);

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail),
            Subject = "Orkut New - Password Reset",
            Body = $"Click the link to reset your password: {resetUrl}\n\nThis link expires in 1 hour.",
            IsBodyHtml = false
        };

        message.To.Add(email);

        await client.SendMailAsync(message, cancellationToken);
    }
}
