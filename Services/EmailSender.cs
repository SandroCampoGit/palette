using System.Net;
using System.Net.Mail;

namespace PulseArtists.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>SMTP sender. If Email:Host isn't configured it silently no-ops, so the
/// app works before SMTP is set up and email failures never break a request.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IConfiguration cfg, ILogger<SmtpEmailSender> log)
    {
        _cfg = cfg; _log = log;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _cfg["Email:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            _log.LogInformation("Email not configured; skipped send to {To}", toEmail);
            return;
        }

        try
        {
            using var msg = new MailMessage
            {
                From = new MailAddress(_cfg["Email:From"] ?? "no-reply@pulseapps.dev",
                                       _cfg["Email:FromName"] ?? "Palette"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);

            using var client = new SmtpClient(host, int.TryParse(_cfg["Email:Port"], out var p) ? p : 587)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_cfg["Email:User"], _cfg["Email:Password"])
            };
            await client.SendMailAsync(msg, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Email to {To} failed", toEmail);
        }
    }
}
