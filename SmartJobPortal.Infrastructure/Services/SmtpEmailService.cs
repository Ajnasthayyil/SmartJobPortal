using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartJobPortal.Application.Interfaces;

namespace SmartJobPortal.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        var host = _config["EmailSettings:Host"];
        var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
        var user = _config["EmailSettings:Username"];
        var pass = _config["EmailSettings:Password"];
        var from = _config["EmailSettings:FromEmail"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _logger.LogWarning("SMTP Email settings are missing. Email was not sent.");
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(from ?? user),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
        };
        mailMessage.To.Add(to);

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            throw;
        }
    }
}
