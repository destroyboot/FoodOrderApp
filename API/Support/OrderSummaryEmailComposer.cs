using API.Localization;
using Core.Data.Entities;
using Core.Data.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.Support;

public interface IOrderSummaryEmailComposer
{
    Task<OrderSummaryEmailModel> ComposeAsync(Order order, OrderInvoiceDocument? invoiceDocument, CancellationToken ct);
}

public sealed class OrderSummaryEmailComposer : IOrderSummaryEmailComposer
{
    private readonly AppDbContext _db;
    private readonly IAppLocalizationFileStore _localization;

    public OrderSummaryEmailComposer(AppDbContext db, IAppLocalizationFileStore localization)
    {
        _db = db;
        _localization = localization;
    }

    public async Task<OrderSummaryEmailModel> ComposeAsync(Order order, OrderInvoiceDocument? invoiceDocument, CancellationToken ct)
    {
        var restaurant = await _db.Restaurants.AsNoTracking().Include(x => x.Settings)
            .FirstOrDefaultAsync(x => x.Id == order.RestaurantId, ct);
        var defaultCulture = restaurant?.Settings?.DefaultCulture?.Trim() ?? "pl-PL";
        var supportedCultures = (restaurant?.Settings?.SupportedCultures ?? defaultCulture)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Append(defaultCulture)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var preferredCulture = string.IsNullOrWhiteSpace(order.CustomerId)
            ? null
            : await _db.Users.Where(x => x.Id == order.CustomerId).Select(x => x.PreferredCulture).FirstOrDefaultAsync(ct);
        var culture = !string.IsNullOrWhiteSpace(preferredCulture) && supportedCultures.Contains(preferredCulture)
            ? preferredCulture!
            : defaultCulture;
        var texts = (await _localization.GetDictionaryAsync(culture, defaultCulture, ct)).Texts;
        string Text(string key, string fallback) => texts.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
        string Format(string key, string fallback) => Text(key, fallback).Replace("{orderNumber}", DisplayNumber(order), StringComparison.Ordinal);

        var menuItemIds = order.Items.Select(x => x.MenuItemId).Distinct().ToArray();
        var names = await _db.MenuItems.AsNoTracking()
            .Where(x => menuItemIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                Name = x.Translations.OrderBy(t => t.Culture == culture ? 0 : t.Culture == defaultCulture ? 1 : 2)
                    .Select(t => t.Name).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        return new OrderSummaryEmailModel(
            Format("email.orderSummary.subject", "Food Order App - Order summary for Order #{orderNumber}"),
            Format("email.orderSummary.title", "Order summary - Order #{orderNumber}"),
            Text("email.orderSummary.date", "Date"), Text("email.orderSummary.restaurant", "Restaurant"),
            Text("email.orderSummary.status", "Status"), Text("email.orderSummary.type", "Type"),
            Text("email.orderSummary.table", "Table"), Text("email.orderSummary.item", "Item"),
            Text("email.orderSummary.quantity", "Quantity"), Text("email.orderSummary.price", "Price"),
            Text("email.orderSummary.total", "Total"), Text("email.orderSummary.note", "Note"),
            Text("email.orderSummary.subtotal", "Subtotal"), Text("email.orderSummary.deliveryFee", "Delivery fee"),
            Text("email.orderSummary.invoice", "Invoice"), Text("email.orderSummary.thankYou", "Thank you for your order!"),
            restaurant?.Name ?? order.Restaurant?.Name ?? "-", StatusText(order.Status, Text), OrderTypeText(order.OrderType, Text),
            order.TableNumber, order.CreatedAt, order.Subtotal, order.DeliveryFee, order.Total, invoiceDocument?.InvoiceNumber,
            order.Items.Select(x => new OrderSummaryEmailLine(names.GetValueOrDefault(x.MenuItemId) ?? $"Menu item #{x.MenuItemId}", x.Quantity, x.UnitPrice, x.Note)).ToList());
    }

    private static string DisplayNumber(Order order) => order.DailyRestaurantOrderNumber?.ToString("0000") ?? order.Id.ToString();

    private static string OrderTypeText(OrderType type, Func<string, string, string> text) => type switch
    {
        OrderType.Table => text("email.orderSummary.type.table", "Table order"),
        OrderType.Takeaway => text("email.orderSummary.type.pickup", "Pickup"),
        OrderType.Delivery => text("email.orderSummary.type.delivery", "Delivery"),
        _ => type.ToString()
    };

    private static string StatusText(OrderStatus status, Func<string, string, string> text) => status switch
    {
        OrderStatus.Pending => text("orders.status.pending", "Pending"),
        OrderStatus.Accepted => text("orders.status.accepted", "Accepted"),
        OrderStatus.SentToKitchen => text("orders.status.sentToKitchen", "Sent to kitchen"),
        OrderStatus.Preparing => text("orders.status.preparing", "Preparing"),
        OrderStatus.ReadyForWaiter => text("orders.status.readyForWaiter", "Ready for waiter"),
        OrderStatus.Ready => text("orders.status.ready", "Ready"),
        OrderStatus.OutForDelivery => text("orders.status.outForDelivery", "Out for delivery"),
        OrderStatus.Delivered => text("orders.status.delivered", "Delivered"),
        OrderStatus.Completed => text("orders.status.completed", "Completed"),
        OrderStatus.Cancelled => text("orders.status.cancelled", "Cancelled"),
        _ => status.ToString()
    };
}
