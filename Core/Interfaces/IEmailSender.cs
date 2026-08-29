using Core.Models;

namespace Core.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IEnumerable<EmailAttachment>? attachments = null,
            CancellationToken ct = default);
    }
}
