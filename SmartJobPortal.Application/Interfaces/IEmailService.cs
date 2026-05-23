using System.Threading.Tasks;

namespace SmartJobPortal.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
}
