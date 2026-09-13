using Core.Data.Entities;
using System.Globalization;
using System.Net;
using System.Text;

namespace API.Support;

internal static class OrderPrintTemplateModelBuilder
{
    public static object BuildOrderModel(
        Order order,
        string displayOrderNumber,
        string? invoiceNumber,
        IReadOnlyDictionary<int, string> itemNames)
    {
        return new
        {
            order = new
            {
                id = order.Id,
                number = displayOrderNumber,
                createdAt = order.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                status = order.Status.ToString(),
                type = order.OrderType.ToString(),
                paymentMethod = order.PaymentMethod.ToString(),
                paymentStatus = order.PaymentStatus.ToString(),
                subtotal = Money(order.Subtotal),
                deliveryFee = Money(order.DeliveryFee),
                total = Money(order.Total)
            },
            restaurant = new
            {
                name = order.Restaurant?.Name ?? "-"
            },
            invoice = new
            {
                number = invoiceNumber ?? string.Empty
            },
            billing = new
            {
                name = BillingName(order),
                taxId = order.BillingDetails?.TaxId ?? "-",
                address = BillingAddress(order)
            },
            itemsTableHtml = BuildItemsTable(order, itemNames)
        };
    }

    private static string BuildItemsTable(Order order, IReadOnlyDictionary<int, string> itemNames)
    {
        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        sb.Append("<th>Item</th><th>Qty</th><th>Unit price</th><th>Total</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var item in order.Items)
        {
            var name = itemNames.GetValueOrDefault(item.MenuItemId, $"Menu item #{item.MenuItemId}");
            sb.Append("<tr>");
            sb.Append($"<td>{Html(name)}</td>");
            sb.Append($"<td>{item.Quantity.ToString(CultureInfo.InvariantCulture)}</td>");
            sb.Append($"<td>{Money(item.UnitPrice)}</td>");
            sb.Append($"<td>{Money(item.UnitPrice * item.Quantity)}</td>");
            sb.Append("</tr>");

            if (!string.IsNullOrWhiteSpace(item.Note))
            {
                sb.Append($"<tr><td colspan=\"4\">Note: {Html(item.Note)}</td></tr>");
            }
        }

        if (order.Items.Count == 0)
        {
            sb.Append("<tr><td colspan=\"4\">No items</td></tr>");
        }

        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string BillingName(Order order)
    {
        var value = order.BillingDetails?.CustomerType == Core.Data.Enums.BillingCustomerType.Company
            ? order.BillingDetails?.CompanyName
            : order.BillingDetails?.PersonName;
        return string.IsNullOrWhiteSpace(value) ? "Customer" : value;
    }

    private static string BillingAddress(Order order)
    {
        var address = string.Join(", ", new[]
        {
            order.BillingDetails?.BillingAddressLine1,
            order.BillingDetails?.BillingAddressLine2,
            order.BillingDetails?.BillingCity,
            order.BillingDetails?.BillingPostalCode,
            order.BillingDetails?.BillingCountry
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

        return string.IsNullOrWhiteSpace(address) ? "-" : address;
    }

    private static string Money(decimal value)
        => value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Html(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
