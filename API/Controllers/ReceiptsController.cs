using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class ReceiptsController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IEmailSender _email;

        public ReceiptsController(IOrderRepository orders, IAsyncQueryExecutor q, IEmailSender email)
        {
            _orders = orders;
            _q = q;
            _email = email;
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

        public record SendReceiptRequest(string? Email);

        /// <summary>
        /// Sends receipt email for an order.
        /// If body.Email is null, uses Order.ReceiptEmail.
        /// Owner-only (JWT or X-Guest-Token).
        /// </summary>
        [HttpPost("{id:int}/send-receipt")]
        [AllowAnonymous]
        public async Task<IActionResult> SendReceipt(int id, [FromBody] SendReceiptRequest req, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();

            var order = await _q.FirstOrDefaultAsync(
                _orders.Query()
                    .Where(o => o.Id == id && o.CustomerId == ownerKey && o.Status != OrderStatus.Draft)
                    .Select(o => new
                    {
                        o.Id,
                        o.Status,
                        o.OrderType,
                        o.TableNumber,
                        o.Subtotal,
                        o.DeliveryFee,
                        o.Total,
                        o.CreatedAt,
                        o.ReceiptEmail,
                        Items = o.Items.Select(i => new { i.MenuItemId, i.Quantity, i.UnitPrice, i.Note }).ToList()
                    }),
                ct);

            if (order is null)
                throw new KeyNotFoundException("Order not found.");

            var toEmail = (req.Email ?? order.ReceiptEmail)?.Trim();
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("No email provided. Set ReceiptEmail in cart meta or pass Email in request body.");

            var html = BuildReceiptHtml(order);

            await _email.SendAsync(
                toEmail: toEmail,
                subject: $"Food Order App – Receipt for Order #{order.Id}",
                htmlBody: html,
                ct: ct);

            return Ok(new { sentTo = toEmail });
        }

        private static string BuildReceiptHtml(dynamic order)
        {
            var sb = new StringBuilder();

            sb.Append("<html><body style='font-family:Arial,sans-serif;'>");
            sb.Append($"<h2>Receipt – Order #{order.Id}</h2>");
            sb.Append($"<p><b>Date:</b> {order.CreatedAt:u}<br/>");
            sb.Append($"<b>Status:</b> {order.Status}<br/>");
            sb.Append($"<b>Type:</b> {order.OrderType}<br/>");
            if (!string.IsNullOrWhiteSpace((string?)order.TableNumber))
                sb.Append($"<b>Table:</b> {order.TableNumber}<br/>");
            sb.Append("</p>");

            sb.Append("<table style='border-collapse:collapse;width:100%;max-width:700px;'>");
            sb.Append("<tr>");
            sb.Append("<th style='border-bottom:1px solid #ccc;text-align:left;padding:6px;'>Item</th>");
            sb.Append("<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>Qty</th>");
            sb.Append("<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>Unit</th>");
            sb.Append("<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>Line</th>");
            sb.Append("</tr>");

            foreach (var i in order.Items)
            {
                decimal line = (decimal)i.UnitPrice * (int)i.Quantity;
                sb.Append("<tr>");
                sb.Append($"<td style='padding:6px;'>MenuItem #{i.MenuItemId}</td>");
                sb.Append($"<td style='padding:6px;text-align:right;'>{i.Quantity}</td>");
                sb.Append($"<td style='padding:6px;text-align:right;'>{((decimal)i.UnitPrice):0.00}</td>");
                sb.Append($"<td style='padding:6px;text-align:right;'>{line:0.00}</td>");
                sb.Append("</tr>");

                if (!string.IsNullOrWhiteSpace((string?)i.Note))
                    sb.Append($"<tr><td colspan='4' style='padding:6px;color:#555;'>Note: {i.Note}</td></tr>");
            }

            sb.Append("</table>");

            sb.Append("<hr/>");
            sb.Append($"<p><b>Subtotal:</b> {((decimal)order.Subtotal):0.00}<br/>");
            sb.Append($"<b>Delivery fee:</b> {((decimal)order.DeliveryFee):0.00}<br/>");
            sb.Append($"<b>Total:</b> {((decimal)order.Total):0.00}</p>");

            sb.Append("<p style='color:#777;'>Thank you for your order!</p>");
            sb.Append("</body></html>");

            return sb.ToString();
        }
    }
}
