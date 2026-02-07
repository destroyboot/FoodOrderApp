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

        public OrderStatusEmailService(UserManager<ApplicationUser> users, IEmailSender email)
        {
            _users = users;
            _email = email;
        }

        public async Task TrySendStatusChangedEmailAsync(
            string ownerKey,
            int orderId,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default)
        {
            // ownerKey for logged-in users is the Identity userId.
            // for guests it is a random token -> FindByIdAsync returns null => no email.
            var user = await _users.FindByIdAsync(ownerKey);
            if (user is null) return; // guest token or unknown

            if (!user.EmailConfirmed) return;
            if (!user.WantsOrderStatusEmails) return;

            var toEmail = user.Email;
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            await _email.SendAsync(
                toEmail: toEmail,
                subject: $"Food Order App – Order #{orderId} status update",
                htmlBody: $@"
                <p>Your order <b>#{orderId}</b> status changed:</p>
                <p><b>{oldStatus}</b> → <b>{newStatus}</b></p>
                <p>Thank you!</p>
            ",
                ct: ct);
        }
    }
}
