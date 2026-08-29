using System.Net;
using System.Text;

namespace API.Support;

public sealed record OrderSummaryEmailLine(string Name, int Quantity, decimal UnitPrice, string? Note);

public sealed record OrderSummaryEmailModel(
    string Subject,
    string Title,
    string DateLabel,
    string RestaurantLabel,
    string StatusLabel,
    string TypeLabel,
    string TableLabel,
    string ItemLabel,
    string QuantityLabel,
    string PriceLabel,
    string TotalLabel,
    string NoteLabel,
    string SubtotalLabel,
    string DeliveryFeeLabel,
    string InvoiceLabel,
    string ThankYou,
    string RestaurantName,
    string Status,
    string OrderType,
    string? TableNumber,
    DateTime CreatedAt,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Total,
    string? InvoiceNumber,
    IReadOnlyList<OrderSummaryEmailLine> Items);

internal static class OrderSummaryEmailBuilder
{
    public static string Build(OrderSummaryEmailModel order)
    {
        var sb = new StringBuilder();
        sb.Append("<html><body style='font-family:Arial,sans-serif;'>");
        sb.Append($"<h2>{Html(order.Title)}</h2>");
        sb.Append($"<p><b>{Html(order.DateLabel)}:</b> {order.CreatedAt:u}<br/>");
        sb.Append($"<b>{Html(order.RestaurantLabel)}:</b> {Html(order.RestaurantName)}<br/>");
        sb.Append($"<b>{Html(order.StatusLabel)}:</b> {Html(order.Status)}<br/>");
        sb.Append($"<b>{Html(order.TypeLabel)}:</b> {Html(order.OrderType)}<br/>");
        if (!string.IsNullOrWhiteSpace(order.TableNumber))
            sb.Append($"<b>{Html(order.TableLabel)}:</b> {Html(order.TableNumber)}<br/>");
        sb.Append("</p><table style='border-collapse:collapse;width:100%;max-width:700px;'><tr>");
        sb.Append($"<th style='border-bottom:1px solid #ccc;text-align:left;padding:6px;'>{Html(order.ItemLabel)}</th>");
        sb.Append($"<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>{Html(order.QuantityLabel)}</th>");
        sb.Append($"<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>{Html(order.PriceLabel)}</th>");
        sb.Append($"<th style='border-bottom:1px solid #ccc;text-align:right;padding:6px;'>{Html(order.TotalLabel)}</th></tr>");

        foreach (var item in order.Items)
        {
            var lineTotal = item.UnitPrice * item.Quantity;
            sb.Append("<tr>");
            sb.Append($"<td style='padding:6px;'>{Html(item.Name)}</td>");
            sb.Append($"<td style='padding:6px;text-align:right;'>{item.Quantity}</td>");
            sb.Append($"<td style='padding:6px;text-align:right;'>{item.UnitPrice:0.00}</td>");
            sb.Append($"<td style='padding:6px;text-align:right;'>{lineTotal:0.00}</td></tr>");
            if (!string.IsNullOrWhiteSpace(item.Note))
                sb.Append($"<tr><td colspan='4' style='padding:6px;color:#555;'>{Html(order.NoteLabel)}: {Html(item.Note)}</td></tr>");
        }

        sb.Append("</table><hr/>");
        sb.Append($"<p><b>{Html(order.SubtotalLabel)}:</b> {order.Subtotal:0.00}<br/>");
        sb.Append($"<b>{Html(order.DeliveryFeeLabel)}:</b> {order.DeliveryFee:0.00}<br/>");
        sb.Append($"<b>{Html(order.TotalLabel)}:</b> {order.Total:0.00}</p>");
        if (!string.IsNullOrWhiteSpace(order.InvoiceNumber))
            sb.Append($"<p><b>{Html(order.InvoiceLabel)}:</b> {Html(order.InvoiceNumber)}</p>");
        sb.Append($"<p style='color:#777;'>{Html(order.ThankYou)}</p></body></html>");
        return sb.ToString();
    }

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
