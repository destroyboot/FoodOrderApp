using Core.Interfaces;
using Core.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _cfg;

        public SmtpEmailSender(IOptions<SmtpSettings> cfg)
        {
            _cfg = cfg.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody,
            IEnumerable<EmailAttachment>? attachments = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Recipient email is required.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            foreach (var attachment in attachments ?? [])
            {
                if (attachment.ContentBytes.Length == 0 || string.IsNullOrWhiteSpace(attachment.FileName))
                    continue;

                bodyBuilder.Attachments.Add(
                    attachment.FileName,
                    attachment.ContentBytes,
                    ContentType.Parse(attachment.ContentType));
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            var socket = _cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
            await client.ConnectAsync(_cfg.Host, _cfg.Port, socket, ct);
            await client.AuthenticateAsync(_cfg.Username, _cfg.Password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
