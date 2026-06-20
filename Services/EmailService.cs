using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace limbo.Services;

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly IConfiguration _config;

    public EmailService(EmailSettings settings, IConfiguration config)
    {
        _settings = settings;
        _config = config;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var appUrl = _config["AppUrl"];
        var link = $"{appUrl}/auth/verifyemail?token={token}";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_settings.From));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = "Verify your email address";
        message.Body = new TextPart("plain")
        {
            Text = $"Please verify your email by clicking the link below:\n\n{link}"
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.SslOnConnect);
        await client.AuthenticateAsync(_settings.User, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}