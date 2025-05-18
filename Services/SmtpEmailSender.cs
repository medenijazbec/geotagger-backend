// File: Services/SmtpEmailSender.cs
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace geotagger_backend.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpClient _client;
    private readonly string _from;

    public SmtpEmailSender(IConfiguration cfg)
    {
        _from = cfg["SMTP_FROM"] ?? "no-reply@example.com";
        var host = cfg["SMTP_HOST"] ?? "localhost";
        var port = int.TryParse(cfg["SMTP_PORT"], out var p) ? p : 25;
        var usr = cfg["SMTP_USER"];
        var pwd = cfg["SMTP_PASS"];

        _client = new SmtpClient(host, port)
        {
            EnableSsl = false,               // MailHog is plain-text
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Credentials = string.IsNullOrWhiteSpace(usr)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(usr, pwd)
        };
    }

    public Task SendAsync(string to, string subject, string htmlBody)
    {
        var msg = new MailMessage(_from, to, subject, htmlBody)
        { IsBodyHtml = true };
        return _client.SendMailAsync(msg);
    }
}
