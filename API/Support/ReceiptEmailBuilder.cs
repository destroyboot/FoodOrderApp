using System.Text;

namespace API.Support
{
    internal static class ReceiptEmailBuilder
    {
        public static string Build(dynamic order)
        {
            var sb = new StringBuilder();

            sb.Append("<html><body style='font-family:Arial,sans-serif;'>");
            sb.Append($"<h2>Receipt - Order #{order.Id}</h2>");
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
