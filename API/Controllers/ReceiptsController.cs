using API.Support;
using Core.Contracts.Orders;
using Core.Models;
using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class ReceiptsController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IEmailSender _email;
        private readonly Infrastructure.Persistence.AppDbContext _db;
        private readonly IOrderSummaryEmailComposer _orderEmails;

        public ReceiptsController(IOrderRepository orders, IAsyncQueryExecutor q, IEmailSender email, Infrastructure.Persistence.AppDbContext db, IOrderSummaryEmailComposer orderEmails)
        {
            _orders = orders;
            _q = q;
            _email = email;
            _db = db;
            _orderEmails = orderEmails;
        }

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken =>
            Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        private string ResolveOwnerKey()
        {
            var key = !string.IsNullOrWhiteSpace(CustomerId) ? CustomerId
                : !string.IsNullOrWhiteSpace(GuestToken) ? GuestToken
                : null;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");

            return key!;
        }

        [HttpPost("{id:int}/send-receipt")]
        [AllowAnonymous]
        public async Task<IActionResult> SendReceipt(int id, [FromBody] SendReceiptRequestDto? req, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();

            var order = await _db.Orders
                .Include(o => o.BillingDetails)
                .Include(o => o.InvoiceDocument)
                .Include(o => o.Items)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == ownerKey && o.Status != OrderStatus.Draft, ct);

            if (order is null)
                throw new KeyNotFoundException("Order not found.");

            var toEmail = (req?.Email ?? order.ReceiptEmail ?? order.BillingDetails?.ReceiptEmail)?.Trim();
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("No email provided. Set ReceiptEmail in cart meta or pass Email in request body.");

            var emailModel = await _orderEmails.ComposeAsync(order, order.InvoiceDocument, ct);
            var attachments = order.InvoiceDocument is null
                ? null
                : new[]
                {
                    new EmailAttachment
                    {
                        FileName = order.InvoiceDocument.FileName,
                        ContentType = order.InvoiceDocument.ContentType,
                        ContentBytes = order.InvoiceDocument.PdfBytes
                    }
                };

            await _email.SendAsync(
                toEmail: toEmail,
                subject: emailModel.Subject,
                htmlBody: OrderSummaryEmailBuilder.Build(emailModel),
                attachments: attachments,
                ct: ct);

            return Ok(new { sentTo = toEmail });
        }
    }
}
