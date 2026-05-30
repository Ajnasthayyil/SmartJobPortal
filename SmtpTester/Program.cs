using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "smtp.gmail.com";
        int port = 587;
        
        string[] passwords = { "zcby ujhm ciqy ygew", "zcbyujhmciqyygew" };
        string[] emails = { "ajnasthayyil123@gmail.com", "talexportal2026@gmail.com", "ajnasthayyil1123@gmail.com" };

        foreach (var email in emails)
        {
            foreach (var password in passwords)
            {
                Console.WriteLine($"Testing MailKit SMTP: {email} | Password: \"{password}\"...");
                
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("SmartJobPortal", email));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = "MailKit Connection Success!";
                
                var bodyBuilder = new BodyBuilder { TextBody = "SMTP verification succeeded!" };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                try
                {
                    await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(email, password);
                    await client.SendAsync(message);
                    Console.WriteLine($"\n===> SUCCESS! Active combination found: {email} | \"{password}\"\n");
                    await client.DisconnectAsync(true);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAILED: {ex.Message}\n");
                }
            }
        }
    }
}
