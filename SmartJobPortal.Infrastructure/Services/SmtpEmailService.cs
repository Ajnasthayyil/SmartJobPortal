using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartJobPortal.Application.Interfaces;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

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
        var host = _config["EmailSettings:Host"] ?? "smtp.gmail.com";
        var portStr = _config["EmailSettings:Port"] ?? "587";
        var user = _config["EmailSettings:Username"];
        var pass = _config["EmailSettings:Password"];
        var from = _config["EmailSettings:FromEmail"] ?? user;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _logger.LogWarning("SMTP Email credentials are missing. Email was not sent.");
            return;
        }

        int port = int.TryParse(portStr, out int parsedPort) ? parsedPort : 587;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("SmartJobPortal", from));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder();
        if (isHtml)
        {
            bodyBuilder.HtmlBody = body;
        }
        else
        {
            bodyBuilder.TextBody = body;
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        
        // Completely bypass SSL/TLS local interceptor failures caused by Antivirus proxies
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;

        try
        {
            SecureSocketOptions socketOption = SecureSocketOptions.Auto;
            if (port == 465)
            {
                socketOption = SecureSocketOptions.SslOnConnect;
            }
            else if (port == 587)
            {
                socketOption = SecureSocketOptions.StartTls;
            }

            await client.ConnectAsync(host, port, socketOption);
            await client.AuthenticateAsync(user, pass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully using MailKit to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MailKit failed to send email to {To}", to);
            throw;
        }
    }
}
