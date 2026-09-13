using Core.Data.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Email
{
    public class OrderStatusEmailService : IOrderStatusEmailService
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly IEmailTemplateRenderer _templates;

        public OrderStatusEmailService(
            UserManager<ApplicationUser> users,
            IEmailSender email,
            IEmailTemplateRenderer templates)
        {
            _users = users;
            _email = email;
            _templates = templates;
        }

        public async Task TrySendStatusChangedEmailAsync(
            string? ownerKey,
            string? fallbackEmail,
            int orderId,
            string displayOrderNumber,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default)
        {
            var toEmail = (fallbackEmail ?? "").Trim();
            var culture = "pl-PL";

            if (!string.IsNullOrWhiteSpace(ownerKey))
            {
                var user = await _users.FindByIdAsync(ownerKey);
                if (user is not null)
                {
                    if (!user.EmailConfirmed)
                        return;

                    if (!user.WantsOrderStatusEmails)
                        return;

                    toEmail = (user.Email ?? "").Trim();
                    culture = string.IsNullOrWhiteSpace(user.PreferredCulture) ? "pl-PL" : user.PreferredCulture.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            try
            {
                await _email.SendAsync(
                    toEmail: toEmail,
                    subject: $"Food Order App - Order #{displayOrderNumber} status update",
                    htmlBody: await _templates.RenderHtmlAsync(
                        "order.status.email",
                        culture,
                        "pl-PL",
                        new
                        {
                            order = new { id = orderId, number = displayOrderNumber },
                            oldStatus = oldStatus.ToString(),
                            newStatus = newStatus.ToString()
                        },
                        ct),
                    ct: ct);
            }
            catch
            {
                // Order status changes should not fail because email delivery had a transient issue.
            }
        }
    }
}
