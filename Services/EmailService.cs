using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using geotagger_backend.Models;
using System.Net.Mail;

namespace geotagger_backend.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly EmailSettings _cfg;
    private readonly ILogger<EmailService> _log;
    public EmailService(IOptions<EmailSettings> cfg, ILogger<EmailService> log)
    {
        _cfg = cfg.Value; _log = log;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var msg = new MimeMessage();
        msg.From.Add(MailboxAddress.Parse(_cfg.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlBody };
        msg.Body = body.ToMessageBody();

        _log.LogInformation("Connecting to {Host}:{Port}", _cfg.Host, _cfg.Port);


        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        await smtp.ConnectAsync(_cfg.Host, _cfg.Port,
            _cfg.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

        if (!string.IsNullOrWhiteSpace(_cfg.User))
            await smtp.AuthenticateAsync(_cfg.User, _cfg.Pass);

        await smtp.SendAsync(msg);
        await smtp.DisconnectAsync(true);
        _log.LogInformation("Mail sent → {Recipient}", to);
    }
}
