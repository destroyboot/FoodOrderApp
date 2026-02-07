using Core.Interfaces;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;

namespace Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _cfg;

        public SmtpEmailSender(IOptions<SmtpSettings> cfg)
        {
            _cfg = cfg.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Recipient email is required.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_cfg.FromName, _cfg.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = htmlBody
            }.ToMessageBody();

            using var client = new SmtpClient();

            // For dev environments with odd certs; you can remove later:
            // client.ServerCertificateValidationCallback = (_, _, _, _) => true;

            var socket = _cfg.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;

            await client.ConnectAsync(_cfg.Host, _cfg.Port, socket, ct);

            // Some servers require this:
            await client.AuthenticateAsync(_cfg.Username, _cfg.Password, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
    }
}
