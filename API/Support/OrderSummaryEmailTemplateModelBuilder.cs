using System.Globalization;
using System.Net;
using System.Text;

namespace API.Support;

internal static class OrderSummaryEmailTemplateModelBuilder
{
    public static object Build(OrderSummaryEmailModel order)
        => new
        {
            title = order.Title,
            labels = new
            {
                date = order.DateLabel,
                restaurant = order.RestaurantLabel,
                status = order.StatusLabel,
                type = order.TypeLabel,
                table = order.TableLabel,
                item = order.ItemLabel,
                quantity = order.QuantityLabel,
                price = order.PriceLabel,
                total = order.TotalLabel,
                note = order.NoteLabel,
                subtotal = order.SubtotalLabel,
                deliveryFee = order.DeliveryFeeLabel,
                invoice = order.InvoiceLabel
            },
            order = new
            {
                number = ExtractOrderNumber(order.Title),
                createdAt = order.CreatedAt.ToString("u", CultureInfo.InvariantCulture),
                status = order.Status,
                type = order.OrderType,
                tableNumber = order.TableNumber ?? string.Empty,
                subtotal = Money(order.Subtotal),
                deliveryFee = Money(order.DeliveryFee),
                total = Money(order.Total)
            },
            restaurant = new { name = order.RestaurantName },
            invoice = new { number = order.InvoiceNumber ?? string.Empty },
            thankYou = order.ThankYou,
            itemsTableHtml = BuildItemsTable(order)
        };

    private static string BuildItemsTable(OrderSummaryEmailModel order)
    {
        var sb = new StringBuilder();
        sb.Append("<table style='border-collapse:collapse;width:100%;max-width:700px;'><tr>");
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
            sb.Append($"<td style='padding:6px;text-align:right;'>{Money(item.UnitPrice)}</td>");
            sb.Append($"<td style='padding:6px;text-align:right;'>{Money(lineTotal)}</td></tr>");
            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                sb.Append($"<tr><td colspan='4' style='padding:6px;color:#555;'>{Html(order.NoteLabel)}: {Html(item.Note)}</td></tr>");
            }
        }

        sb.Append("</table>");
        return sb.ToString();
    }

    private static string ExtractOrderNumber(string title)
    {
        var hashIndex = title.LastIndexOf('#');
        return hashIndex >= 0 && hashIndex < title.Length - 1
            ? title[(hashIndex + 1)..].Trim()
            : string.Empty;
    }

    private static string Money(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Html(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
