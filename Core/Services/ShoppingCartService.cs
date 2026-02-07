using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Contracts.Orders;
using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;

namespace Core.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly IOrderRepository _orders;
        private readonly IMenuItemRepository _menuItems;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;
        private readonly IOrderEventPublisher _events;
        private readonly INotificationService _notifications;

        public ShoppingCartService(
            IOrderRepository orders,
            IMenuItemRepository menuItems,
            IAsyncQueryExecutor q,
            IUnitOfWork uow,
            IOrderEventPublisher events,
            INotificationService notifications)
                {
                    _orders = orders;
                    _menuItems = menuItems;
                    _q = q;
                    _uow = uow;
                    _events = events;
                    _notifications = notifications;
                }

        public async Task<CartCreateResponseDto> CreateCartAsync(string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var ownerKey = NormalizeOwnerKey(customerId, guestToken);

            var existing = await _q.FirstOrDefaultAsync(
                _orders.Query(tracked: true)
                    .Where(o => o.Status == OrderStatus.Draft && o.CustomerId == ownerKey),
                ct);

            if (existing is not null)
                return new CartCreateResponseDto { CartId = existing.Id };

            var cart = new Order
            {
                CustomerId = ownerKey,
                Status = OrderStatus.Draft,
                OrderType = OrderType.Table,
                PaymentMethod = PaymentMethod.AtCounter,
                PaymentStatus = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow
            };

            await _orders.AddAsync(cart, ct);
            await _uow.SaveChangesAsync(ct);

            return new CartCreateResponseDto { CartId = cart.Id };
        }

        public async Task<CartResponseDto> GetCartAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartRead(cartId, customerId, guestToken, ct);

            return new CartResponseDto
            {
                CartId = cart.Id,
                OrderType = cart.OrderType,
                TableNumber = cart.TableNumber,
                RestaurantId = cart.RestaurantId,
                PaymentMethod = cart.PaymentMethod,
                ScheduledFor = cart.ScheduledFor,
                ReceiptEmail = cart.ReceiptEmail,
                Items = cart.Items.Select(i => new CartLineResponseDto
                {
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    Note = i.Note
                }).ToList()
            };
        }

        public async Task SetMetaAsync(int cartId, CartSetMetaDto dto, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);

            if (dto.OrderType == OrderType.Table && string.IsNullOrWhiteSpace(dto.TableNumber))
                throw new InvalidOperationException("TableNumber is required for table orders.");

            cart.OrderType = dto.OrderType;
            cart.TableNumber = string.IsNullOrWhiteSpace(dto.TableNumber) ? null : dto.TableNumber.Trim();
            cart.RestaurantId = dto.RestaurantId;

            cart.PaymentMethod = dto.PaymentMethod;
            cart.ScheduledFor = dto.ScheduledFor;
            cart.ReceiptEmail = string.IsNullOrWhiteSpace(dto.ReceiptEmail) ? null : dto.ReceiptEmail.Trim();

            if (cart.OrderType != OrderType.Delivery)
                cart.DeliveryState = null;

            await _uow.SaveChangesAsync(ct);
        }

        public async Task UpdateItemsAsync(int cartId, CartUpdateItemsDto dto, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);

            if (dto.Items is null)
                throw new InvalidOperationException("Items are required.");

            var normalized = dto.Items
                .Where(i => i is not null)
                .Select(i => new CartItemDto
                {
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim()
                })
                .ToList();

            if (normalized.Any(i => i.MenuItemId <= 0))
                throw new InvalidOperationException("MenuItemId must be > 0.");

            if (normalized.Any(i => i.Quantity <= 0))
                throw new InvalidOperationException("Quantity must be > 0.");

            var combined = normalized
                .GroupBy(i => i.MenuItemId)
                .Select(g => new CartItemDto
                {
                    MenuItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity),
                    Note = g.Select(x => x.Note).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
                })
                .ToList();

            var ids = combined.Select(x => x.MenuItemId).Distinct().ToList();

            var availableIds = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(m => ids.Contains(m.Id) && m.IsAvailable)
                    .Select(m => m.Id),
                ct);

            if (availableIds.Count != ids.Count)
                throw new InvalidOperationException("One or more menu items do not exist or are not available.");

            cart.Items.Clear();

            foreach (var item in combined)
            {
                cart.Items.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    Note = item.Note,
                    UnitPrice = 0m // draft
                });
            }

            await _uow.SaveChangesAsync(ct);
        }

        public async Task ClearAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);
            cart.Items.Clear();
            await _uow.SaveChangesAsync(ct);
        }

        public async Task<CartPreviewResponseDto> PreviewAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartRead(cartId, customerId, guestToken, ct);

            if (cart.Items.Count == 0)
            {
                return new CartPreviewResponseDto
                {
                    Subtotal = 0m,
                    DeliveryFee = 0m,
                    Total = 0m,
                    EstimatedPreparationMinutes = 0,
                    EstimatedReadyAt = null
                };
            }

            var ids = cart.Items.Select(i => i.MenuItemId).Distinct().ToList();

            var prices = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(m => ids.Contains(m.Id))
                    .Select(m => new { m.Id, m.CurrentPrice, m.IsAvailable }),
                ct);

            if (prices.Any(p => !p.IsAvailable) || prices.Count != ids.Count)
                throw new InvalidOperationException("One or more menu items are no longer available.");

            var priceMap = prices.ToDictionary(x => x.Id, x => x.CurrentPrice);

            var subtotal = cart.Items.Sum(i => i.Quantity * priceMap[i.MenuItemId]);

            var deliveryFee = CalculateDeliveryFee(cart, subtotal);
            var total = subtotal + deliveryFee;

            var minutes = EstimatePreparationMinutes(cart);
            var readyAt = minutes > 0 ? DateTime.UtcNow.AddMinutes(minutes) : (DateTime?)null;

            return new CartPreviewResponseDto
            {
                Subtotal = RoundMoney(subtotal),
                DeliveryFee = RoundMoney(deliveryFee),
                Total = RoundMoney(total),
                EstimatedPreparationMinutes = minutes,
                EstimatedReadyAt = readyAt
            };
        }

        public async Task<FinalizeResponseDto> FinalizeAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);

            if (cart.Items.Count == 0)
                throw new InvalidOperationException("Cart is empty.");

            if (cart.OrderType == OrderType.Table && string.IsNullOrWhiteSpace(cart.TableNumber))
                throw new InvalidOperationException("TableNumber is required for table orders.");

            var ids = cart.Items.Select(i => i.MenuItemId).Distinct().ToList();

            var items = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(m => ids.Contains(m.Id))
                    .Select(m => new { m.Id, m.CurrentPrice, m.IsAvailable }),
                ct);

            if (items.Count != ids.Count || items.Any(i => !i.IsAvailable))
                throw new InvalidOperationException("One or more menu items do not exist or are not available.");

            var priceMap = items.ToDictionary(x => x.Id, x => x.CurrentPrice);

            foreach (var oi in cart.Items)
                oi.UnitPrice = priceMap[oi.MenuItemId];

            var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
            var deliveryFee = CalculateDeliveryFee(cart, subtotal);
            var total = subtotal + deliveryFee;

            cart.Subtotal = RoundMoney(subtotal);
            cart.DeliveryFee = RoundMoney(deliveryFee);
            cart.Total = RoundMoney(total);

            var minutes = EstimatePreparationMinutes(cart);
            cart.EstimatedPreparationMinutes = minutes;
            cart.EstimatedReadyAt = minutes > 0 ? DateTime.UtcNow.AddMinutes(minutes) : null;

            // Transition Draft -> Pending
            cart.Status = OrderStatus.Pending;

            cart.PaymentStatus = cart.PaymentMethod == PaymentMethod.InApp
                ? PaymentStatus.Pending
                : PaymentStatus.Unpaid;

            await _uow.SaveChangesAsync(ct);

            await _notifications.CreateAsync(
                ownerKey: cart.CustomerId!,
                type: NotificationType.OrderCreated,
                title: "Order placed",
                body: $"Order #{cart.Id} is now {cart.Status}. Total: {cart.Total:0.00}",
                payloadJson: null,
                ct: ct);

            // ✅ SignalR event publish (via interface)
            await _events.PublishNewOrderAsync(
                orderId: cart.Id,
                status: cart.Status,
                orderType: cart.OrderType,
                tableNumber: cart.TableNumber,
                total: cart.Total,
                createdAt: cart.CreatedAt,
                ct: ct);

            return new FinalizeResponseDto
            {
                OrderId = cart.Id,
                Status = cart.Status,
                Total = cart.Total,
                CreatedAt = cart.CreatedAt
            };
        }

        // ---------------- Helpers ----------------

        private static string NormalizeOwnerKey(string? customerId, string? guestToken)
        {
            var key = !string.IsNullOrWhiteSpace(customerId) ? customerId.Trim()
                : !string.IsNullOrWhiteSpace(guestToken) ? guestToken.Trim()
                : null;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("customerId or guestToken is required.");

            return key!;
        }

        private async Task<Order> LoadDraftCartTracked(int cartId, string? customerId, string? guestToken, CancellationToken ct)
        {
            var ownerKey = NormalizeOwnerKey(customerId, guestToken);

            var cart = await _q.FirstOrDefaultAsync(
                _orders.Query(tracked: true).Where(o => o.Id == cartId),
                ct);

            if (cart is null)
                throw new InvalidOperationException("Cart not found.");

            if (cart.Status != OrderStatus.Draft)
                throw new InvalidOperationException("Cart is not editable (not Draft).");

            if (!string.Equals(cart.CustomerId, ownerKey, StringComparison.Ordinal))
                throw new InvalidOperationException("Not allowed to access this cart.");

            return cart;
        }

        private async Task<Order> LoadDraftCartRead(int cartId, string? customerId, string? guestToken, CancellationToken ct)
        {
            var ownerKey = NormalizeOwnerKey(customerId, guestToken);

            var cart = await _q.FirstOrDefaultAsync(
                _orders.Query(tracked: false).Where(o => o.Id == cartId),
                ct);

            if (cart is null)
                throw new InvalidOperationException("Cart not found.");

            if (cart.Status != OrderStatus.Draft)
                throw new InvalidOperationException("Cart is not editable (not Draft).");

            if (!string.Equals(cart.CustomerId, ownerKey, StringComparison.Ordinal))
                throw new InvalidOperationException("Not allowed to access this cart.");

            return cart;
        }

        private static decimal CalculateDeliveryFee(Order cart, decimal subtotal)
        {
            if (cart.OrderType != OrderType.Delivery)
                return 0m;

            return 8.00m; // placeholder
        }

        private static int EstimatePreparationMinutes(Order cart)
        {
            var qty = cart.Items.Sum(i => i.Quantity);
            return qty == 0 ? 0 : 10 + (2 * qty);
        }

        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
