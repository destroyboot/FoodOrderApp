using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Contracts.Orders;
using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;

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
        private readonly IRestaurantRepository _restaurants;
        private readonly IRestaurantTableRepository _restaurantTables;

        public ShoppingCartService(
            IOrderRepository orders,
            IMenuItemRepository menuItems,
            IAsyncQueryExecutor q,
            IUnitOfWork uow,
            IOrderEventPublisher events,
            INotificationService notifications,
            IRestaurantRepository restaurants,
            IRestaurantTableRepository restaurantTables)
                {
                    _orders = orders;
                    _menuItems = menuItems;
                    _q = q;
                    _uow = uow;
                    _events = events;
                    _notifications = notifications;
                    _restaurants = restaurants;
                    _restaurantTables = restaurantTables;
                }

        public async Task<CartCreateResponseDto> CreateCartAsync(string? customerId, string? guestToken, int? restaurantId = null, CancellationToken ct = default)
        {
            var ownerKey = NormalizeOwnerKey(customerId, guestToken);

            var drafts = await _q.ToListAsync(
                _orders.Query(tracked: true)
                    .Where(o => o.Status == OrderStatus.Draft && o.CustomerId == ownerKey)
                    .OrderByDescending(o => o.CreatedAt),
                ct);

            if (restaurantId.HasValue)
            {
                var existingForRestaurant = drafts.FirstOrDefault(o => o.RestaurantId == restaurantId.Value);
                if (existingForRestaurant is not null)
                    return new CartCreateResponseDto { CartId = existingForRestaurant.Id };

                var restaurantDraftIdsWithItems = await _q.ToListAsync(
                    _orders.Query()
                        .Where(o => o.Status == OrderStatus.Draft
                                    && o.CustomerId == ownerKey
                                    && o.RestaurantId.HasValue
                                    && o.Items.Any())
                        .Select(o => o.RestaurantId!.Value)
                        .Distinct(),
                    ct);

                var restaurantDraftCount = restaurantDraftIdsWithItems.Count;

                if (string.IsNullOrWhiteSpace(customerId) && restaurantDraftCount > 0)
                    throw new InvalidOperationException("You already have an active cart in another restaurant. Remove that cart or sign in to keep more than one.");

                if (restaurantDraftCount >= 4)
                    throw new InvalidOperationException("You cannot have more than 4 active carts.");
            }
            else
            {
                var orphanDraft = drafts.FirstOrDefault(o => o.RestaurantId == null);
                if (orphanDraft is not null)
                    return new CartCreateResponseDto { CartId = orphanDraft.Id };
            }

            var cart = new Order
            {
                CustomerId = ownerKey,
                Status = OrderStatus.Draft,
                RestaurantId = restaurantId,
                OrderType = OrderType.Table,
                PaymentMethod = PaymentMethod.AtCounter,
                PaymentStatus = PaymentStatus.Unpaid,
                CreatedAt = DateTime.UtcNow
            };

            await _orders.AddAsync(cart, ct);
            await _uow.SaveChangesAsync(ct);

            return new CartCreateResponseDto { CartId = cart.Id };
        }

        public async Task<IReadOnlyList<ActiveCartSummaryDto>> GetActiveCartsAsync(string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var ownerKey = NormalizeOwnerKey(customerId, guestToken);

            var carts = await _q.ToListAsync(
                _orders.Query(tracked: false)
                    .Where(o => o.Status == OrderStatus.Draft && o.CustomerId == ownerKey)
                    .Where(o => o.RestaurantId != null)
                    .Where(o => o.Items.Any())
                    .Select(o => new ActiveCartSummaryDto
                    {
                        CartId = o.Id,
                        RestaurantId = o.RestaurantId,
                        RestaurantName = null,
                        ItemCount = o.Items.Count,
                        TotalQuantity = o.Items.Sum(i => i.Quantity),
                        CreatedAt = o.CreatedAt
                    })
                    .OrderByDescending(o => o.CreatedAt),
                ct);

            var deduped = carts
                .GroupBy(x => x.RestaurantId)
                .Select(g => g.OrderByDescending(x => x.CreatedAt).First())
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var restaurantIds = deduped
                .Where(x => x.RestaurantId.HasValue)
                .Select(x => x.RestaurantId!.Value)
                .Distinct()
                .ToList();

            var restaurantNameMap = restaurantIds.Count == 0
                ? new Dictionary<int, string>()
                : (await _q.ToListAsync(
                    _restaurants.Query()
                        .Where(x => restaurantIds.Contains(x.Id))
                        .Select(x => new { x.Id, x.Name }),
                    ct))
                    .ToDictionary(x => x.Id, x => x.Name);

            foreach (var cart in deduped)
            {
                if (cart.RestaurantId.HasValue && restaurantNameMap.TryGetValue(cart.RestaurantId.Value, out var restaurantName))
                {
                    cart.RestaurantName = restaurantName;
                }
            }

            return deduped;
        }

        public async Task<CartResponseDto> GetCartAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartRead(cartId, customerId, guestToken, ct);
            var menuItemIds = cart.Items.Select(x => x.MenuItemId).Distinct().ToArray();
            var menuItems = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(x => menuItemIds.Contains(x.Id))
                    .Select(x => new
                    {
                        x.Id,
                        x.CurrentPrice,
                        Name = x.Translations
                            .OrderBy(t => t.Culture == "pl-PL" ? 0 : 1)
                            .Select(t => t.Name)
                            .FirstOrDefault()
                    }),
                ct);
            var menuItemMap = menuItems.ToDictionary(x => x.Id);

            return new CartResponseDto
            {
                CartId = cart.Id,
                OrderType = cart.OrderType,
                RestaurantTableId = cart.RestaurantTableId,
                TableNumber = cart.TableNumber,
                RestaurantId = cart.RestaurantId,
                PaymentMethod = cart.PaymentMethod,
                ScheduledFor = cart.ScheduledFor,
                ReservationId = cart.ReservationId,
                PickupContactName = cart.PickupContactName,
                PickupPhone = cart.PickupPhone,
                PickupNote = cart.PickupNote,
                DeliveryContactName = cart.DeliveryContactName,
                DeliveryPhone = cart.DeliveryPhone,
                DeliveryAddressLine1 = cart.DeliveryAddressLine1,
                DeliveryAddressLine2 = cart.DeliveryAddressLine2,
                DeliveryCity = cart.DeliveryCity,
                DeliveryPostalCode = cart.DeliveryPostalCode,
                DeliveryCountry = cart.DeliveryCountry,
                DeliveryNote = cart.DeliveryNote,
                ReceiptEmail = cart.ReceiptEmail,
                BillingDetails = cart.BillingDetails is null ? null : new OrderBillingDetailsDto
                {
                    CustomerType = cart.BillingDetails.CustomerType,
                    InvoiceStatus = cart.BillingDetails.InvoiceStatus,
                    ReceiptEmail = cart.BillingDetails.ReceiptEmail,
                    PersonName = cart.BillingDetails.PersonName,
                    CompanyName = cart.BillingDetails.CompanyName,
                    TaxId = cart.BillingDetails.TaxId,
                    BillingAddressLine1 = cart.BillingDetails.BillingAddressLine1,
                    BillingAddressLine2 = cart.BillingDetails.BillingAddressLine2,
                    BillingCity = cart.BillingDetails.BillingCity,
                    BillingPostalCode = cart.BillingDetails.BillingPostalCode,
                    BillingCountry = cart.BillingDetails.BillingCountry
                },
                Items = cart.Items.Select(i => new CartLineResponseDto
                {
                    LineId = i.Id,
                    MenuItemId = i.MenuItemId,
                    MenuItemName = menuItemMap.GetValueOrDefault(i.MenuItemId)?.Name,
                    Quantity = i.Quantity,
                    UnitPrice = menuItemMap.GetValueOrDefault(i.MenuItemId)?.CurrentPrice ?? 0m,
                    LineTotal = (menuItemMap.GetValueOrDefault(i.MenuItemId)?.CurrentPrice + i.ExtraCharge ?? 0m) * i.Quantity,
                    Note = i.Note,
                    ExtraCharge = i.ExtraCharge,
                    RemovedIngredientIds = ParseCustomization(i.CustomizationsJson).RemovedIngredientIds,
                    AddedIngredientIds = ParseCustomization(i.CustomizationsJson).AddedIngredientIds
                }).ToList()
            };
        }

        public async Task SetMetaAsync(int cartId, CartSetMetaDto dto, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);

            if (dto.OrderType == OrderType.Table && !dto.RestaurantTableId.HasValue)
                throw new InvalidOperationException("RestaurantTableId is required for table orders.");

            if (!dto.RestaurantId.HasValue || dto.RestaurantId.Value <= 0)
                throw new InvalidOperationException("Restaurant is required.");

            var restaurant = await _q.FirstOrDefaultAsync(
                _restaurants.Query().Where(r => r.Id == dto.RestaurantId.Value && r.IsActive),
                ct);

            if (restaurant is null)
                throw new InvalidOperationException("Restaurant does not exist or is inactive.");

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            EnsureOrderTypeEnabled(settings, dto.OrderType);
            EnsurePaymentMethodEnabled(settings, dto.OrderType, dto.PaymentMethod);

            cart.OrderType = dto.OrderType;
            cart.RestaurantId = dto.RestaurantId;

            if (cart.OrderType == OrderType.Table)
            {
                if (!dto.RestaurantTableId.HasValue || dto.RestaurantTableId.Value <= 0)
                    throw new InvalidOperationException("RestaurantTableId is required for table orders.");

                var table = await _q.FirstOrDefaultAsync(
                    _restaurantTables.Query()
                        .Where(t => t.Id == dto.RestaurantTableId.Value
                                    && t.RestaurantId == dto.RestaurantId.Value
                                    && t.IsActive),
                    ct);

                if (table is null)
                    throw new InvalidOperationException("Table does not exist or is inactive for this restaurant.");

                cart.RestaurantTableId = table.Id;
                cart.TableNumber = table.Label;
            }
            else
            {
                cart.RestaurantTableId = null;
                cart.TableNumber = null;
            }

            cart.PaymentMethod = dto.PaymentMethod;
            cart.ScheduledFor = cart.OrderType == OrderType.Takeaway || dto.ReservationId.HasValue
                ? dto.ScheduledFor
                : null;
            cart.ReservationId = dto.ReservationId;
            cart.PickupContactName = cart.OrderType == OrderType.Takeaway ? TrimOrNull(dto.PickupContactName) : null;
            cart.PickupPhone = cart.OrderType == OrderType.Takeaway ? TrimOrNull(dto.PickupPhone) : null;
            cart.PickupNote = cart.OrderType == OrderType.Takeaway ? TrimOrNull(dto.PickupNote) : null;
            cart.DeliveryContactName = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryContactName) : null;
            cart.DeliveryPhone = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryPhone) : null;
            cart.DeliveryAddressLine1 = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryAddressLine1) : null;
            cart.DeliveryAddressLine2 = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryAddressLine2) : null;
            cart.DeliveryCity = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryCity) : null;
            cart.DeliveryPostalCode = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryPostalCode) : null;
            cart.DeliveryCountry = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryCountry) : null;
            cart.DeliveryNote = cart.OrderType == OrderType.Delivery ? TrimOrNull(dto.DeliveryNote) : null;
            cart.ReceiptEmail = string.IsNullOrWhiteSpace(dto.ReceiptEmail) ? null : dto.ReceiptEmail.Trim();
            ValidateReceiptEmail(cart.ReceiptEmail);
            UpsertBillingDetails(cart, dto);

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
                    LineId = i.LineId,
                    MenuItemId = i.MenuItemId,
                    Quantity = i.Quantity,
                    Note = string.IsNullOrWhiteSpace(i.Note) ? null : i.Note.Trim(),
                    RemovedIngredientIds = i.RemovedIngredientIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>(),
                    AddedIngredientIds = i.AddedIngredientIds?.Where(x => x > 0).Distinct().ToList() ?? new List<int>()
                })
                .ToList();

            if (normalized.Any(i => i.MenuItemId <= 0))
                throw new InvalidOperationException("MenuItemId must be > 0.");

            if (normalized.Any(i => i.Quantity <= 0))
                throw new InvalidOperationException("Quantity must be > 0.");

            var combined = normalized
                .GroupBy(CreateCustomizationSignature)
                .Select(g => new CartItemDto
                {
                    MenuItemId = g.First().MenuItemId,
                    Quantity = g.Sum(x => x.Quantity),
                    Note = g.Select(x => x.Note).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                    RemovedIngredientIds = g.First().RemovedIngredientIds,
                    AddedIngredientIds = g.First().AddedIngredientIds
                })
                .ToList();

            var ids = combined.Select(x => x.MenuItemId).Distinct().ToList();

            var menuItems = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(m => ids.Contains(m.Id) && m.IsAvailable)
                    .Select(m => new MenuItemCustomizationSnapshot
                    {
                        Id = m.Id,
                        EnableIngredientSwap = m.EnableIngredientSwap,
                        ExtraIngredientPrice = m.Category != null && m.Category.Restaurant != null && m.Category.Restaurant.Settings != null
                            ? m.Category.Restaurant.Settings.ExtraIngredientPrice
                            : 0m,
                        Ingredients = m.Ingredients.Select(i => new IngredientCustomizationSnapshot
                        {
                            IngredientId = i.IngredientId,
                            IsDefault = i.IsDefault,
                            IsRemovable = i.IsRemovable,
                            IsSubstitute = i.IsSubstitute,
                            Name = i.Ingredient != null
                                ? (i.Ingredient.Translations.Select(t => t.Name).FirstOrDefault() ?? $"Ingredient #{i.IngredientId}")
                                : $"Ingredient #{i.IngredientId}"
                        }).ToList()
                    }),
                ct);

            if (menuItems.Count != ids.Count)
                throw new InvalidOperationException("One or more menu items do not exist or are not available.");

            var itemMap = menuItems.ToDictionary(x => x.Id);

            cart.Items.Clear();

            foreach (var item in combined)
            {
                var menuItem = itemMap[item.MenuItemId];
                var customization = ValidateAndBuildCustomization(menuItem, item.RemovedIngredientIds, item.AddedIngredientIds);
                cart.Items.Add(new OrderItem
                {
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    Note = CombineNotes(item.Note, customization.DisplayNote),
                    UnitPrice = 0m,
                    ExtraCharge = customization.ExtraCharge,
                    CustomizationsJson = customization.Json
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

        public async Task DeleteAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default)
        {
            var cart = await LoadDraftCartTracked(cartId, customerId, guestToken, ct);
            await _orders.RemoveAsync(cart, ct);
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

            var extraChargeTotal = cart.Items.Sum(i => i.Quantity * i.ExtraCharge);
            var subtotal = cart.Items.Sum(i => i.Quantity * (priceMap[i.MenuItemId] + i.ExtraCharge));

            var settings = await LoadSettingsForCartAsync(cart, ct);
            var deliveryFee = CalculateDeliveryFee(cart, subtotal, settings);
            var total = subtotal + deliveryFee;

            var minutes = EstimatePreparationMinutes(cart, settings);
            var readyAt = minutes > 0 ? DateTime.UtcNow.AddMinutes(minutes) : (DateTime?)null;

            return new CartPreviewResponseDto
            {
                Subtotal = RoundMoney(subtotal),
                ExtraChargeTotal = RoundMoney(extraChargeTotal),
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
                throw new InvalidOperationException("Table is required for table orders.");

            if (cart.OrderType == OrderType.Takeaway)
            {
                if (string.IsNullOrWhiteSpace(cart.PickupContactName))
                    throw new InvalidOperationException("Pickup contact name is required for pickup orders.");

                if (string.IsNullOrWhiteSpace(cart.PickupPhone))
                    throw new InvalidOperationException("Pickup phone is required for pickup orders.");

                if (cart.ScheduledFor.HasValue && cart.ScheduledFor.Value <= DateTime.UtcNow)
                    throw new InvalidOperationException("Pickup time must be in the future.");
            }

            if (cart.OrderType == OrderType.Delivery)
            {
                if (string.IsNullOrWhiteSpace(cart.DeliveryContactName))
                    throw new InvalidOperationException("Delivery contact name is required for delivery orders.");

                if (string.IsNullOrWhiteSpace(cart.DeliveryPhone))
                    throw new InvalidOperationException("Delivery phone is required for delivery orders.");

                if (string.IsNullOrWhiteSpace(cart.DeliveryAddressLine1))
                    throw new InvalidOperationException("Delivery address line 1 is required for delivery orders.");

                if (string.IsNullOrWhiteSpace(cart.DeliveryCity))
                    throw new InvalidOperationException("Delivery city is required for delivery orders.");

                if (string.IsNullOrWhiteSpace(cart.DeliveryPostalCode))
                    throw new InvalidOperationException("Delivery postal code is required for delivery orders.");
            }

            if (!cart.RestaurantId.HasValue)
                throw new InvalidOperationException("Restaurant is required.");

            var restaurant = await _q.FirstOrDefaultAsync(
                _restaurants.Query().Where(r => r.Id == cart.RestaurantId.Value && r.IsActive),
                ct);

            if (restaurant is null)
                throw new InvalidOperationException("Restaurant does not exist or is inactive.");

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            EnsureOrderTypeEnabled(settings, cart.OrderType);
            EnsurePaymentMethodEnabled(settings, cart.OrderType, cart.PaymentMethod);
            await EnsureReservationScheduleValidAsync(cart, settings, ct);

            var ids = cart.Items.Select(i => i.MenuItemId).Distinct().ToList();

            var items = await _q.ToListAsync(
                _menuItems.Query()
                    .Where(m => ids.Contains(m.Id))
                    .Select(m => new
                    {
                        Entity = new MenuItemCustomizationSnapshot
                        {
                            Id = m.Id,
                            EnableIngredientSwap = m.EnableIngredientSwap,
                            ExtraIngredientPrice = m.Category != null && m.Category.Restaurant != null && m.Category.Restaurant.Settings != null
                                ? m.Category.Restaurant.Settings.ExtraIngredientPrice
                                : 0m,
                            Ingredients = m.Ingredients.Select(i => new IngredientCustomizationSnapshot
                            {
                                IngredientId = i.IngredientId,
                                IsDefault = i.IsDefault,
                                IsRemovable = i.IsRemovable,
                                IsSubstitute = i.IsSubstitute,
                                Name = i.Ingredient != null
                                    ? (i.Ingredient.Translations.Select(t => t.Name).FirstOrDefault() ?? $"Ingredient #{i.IngredientId}")
                                    : $"Ingredient #{i.IngredientId}"
                            }).ToList()
                        },
                        m.Id,
                        m.CurrentPrice,
                        m.IsAvailable,
                        RestaurantId = m.Category!.RestaurantId
                    }),
                ct);

            if (items.Count != ids.Count || items.Any(i => !i.IsAvailable))
                throw new InvalidOperationException("One or more menu items do not exist or are not available.");

            if (items.Any(i => i.RestaurantId != cart.RestaurantId.Value))
                throw new InvalidOperationException("All cart items must belong to the selected restaurant.");

            var priceMap = items.ToDictionary(x => x.Id, x => x.CurrentPrice);
            var menuItemMap = items.ToDictionary(x => x.Id, x => x.Entity);

            foreach (var oi in cart.Items)
            {
                var customization = ParseCustomization(oi.CustomizationsJson);
                var recalculated = ValidateAndBuildCustomization(menuItemMap[oi.MenuItemId], customization.RemovedIngredientIds, customization.AddedIngredientIds);
                oi.ExtraCharge = recalculated.ExtraCharge;
                oi.Note = CombineNotes(ExtractUserNote(oi.Note, recalculated.DisplayNote), recalculated.DisplayNote);
                oi.CustomizationsJson = recalculated.Json;
                oi.UnitPrice = priceMap[oi.MenuItemId] + recalculated.ExtraCharge;
            }

            var subtotal = cart.Items.Sum(i => i.Quantity * i.UnitPrice);
            var deliveryFee = CalculateDeliveryFee(cart, subtotal, settings);
            var total = subtotal + deliveryFee;

            cart.Subtotal = RoundMoney(subtotal);
            cart.DeliveryFee = RoundMoney(deliveryFee);
            cart.Total = RoundMoney(total);

            var minutes = EstimatePreparationMinutes(cart, settings);
            cart.EstimatedPreparationMinutes = minutes;
            cart.EstimatedReadyAt = minutes > 0 ? DateTime.UtcNow.AddMinutes(minutes) : null;
            AssignDailyRestaurantOrderNumber(cart);

            cart.Status = OrderStatus.Pending;

            cart.PaymentStatus = cart.PaymentMethod == PaymentMethod.InApp
                ? PaymentStatus.Paid
                : PaymentStatus.Unpaid;

            await _uow.SaveChangesAsync(ct);

            await _notifications.CreateAsync(
                ownerKey: cart.CustomerId!,
                type: NotificationType.OrderCreated,
                title: "Order placed",
                body: $"Your order #{FormatDisplayOrderNumber(cart)} has been placed. Total: {cart.Total:0.00}",
                payloadJson: JsonSerializer.Serialize(new
                {
                    type = "order-created",
                    orderId = cart.Id,
                    status = cart.Status.ToString(),
                    url = "/orders"
                }),
                ct: ct);

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

        private async Task<RestaurantSettings?> LoadSettingsForCartAsync(Order cart, CancellationToken ct)
        {
            if (!cart.RestaurantId.HasValue)
                return null;

            var restaurant = await _q.FirstOrDefaultAsync(
                _restaurants.Query().Where(r => r.Id == cart.RestaurantId.Value && r.IsActive),
                ct);

            return restaurant?.Settings ?? new RestaurantSettings { RestaurantId = cart.RestaurantId.Value };
        }

        private static Task EnsureReservationScheduleValidAsync(Order cart, RestaurantSettings settings, CancellationToken ct)
        {
            if (!cart.ReservationId.HasValue)
                return Task.CompletedTask;

            if (!settings.EnableReservations)
                throw new InvalidOperationException("Reservations are not enabled for this restaurant.");

            if (cart.PaymentMethod != PaymentMethod.InApp && settings.ReservationRequiresInAppPayment)
                throw new InvalidOperationException("Reservation pre-orders require in-app payment.");

            if (!cart.ScheduledFor.HasValue)
                throw new InvalidOperationException("ScheduledFor is required for reservation pre-orders.");

            if (cart.ScheduledFor.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Reservation pre-orders must be scheduled in the future.");

            return Task.CompletedTask;
        }

        private static void UpsertBillingDetails(Order cart, CartSetMetaDto dto)
        {
            if (dto.BillingDetails is null || dto.BillingDetails.InvoiceStatus == InvoiceStatus.NotRequested)
            {
                cart.BillingDetails = null;
                return;
            }

            cart.BillingDetails ??= new OrderBillingDetails();
            cart.BillingDetails.CustomerType = dto.BillingDetails.CustomerType;
            cart.BillingDetails.InvoiceStatus = dto.BillingDetails.InvoiceStatus;
            cart.BillingDetails.ReceiptEmail = string.IsNullOrWhiteSpace(dto.BillingDetails.ReceiptEmail)
                ? dto.ReceiptEmail
                : dto.BillingDetails.ReceiptEmail.Trim();
            cart.BillingDetails.PersonName = TrimOrNull(dto.BillingDetails.PersonName);
            cart.BillingDetails.CompanyName = TrimOrNull(dto.BillingDetails.CompanyName);
            cart.BillingDetails.TaxId = TrimOrNull(dto.BillingDetails.TaxId);
            cart.BillingDetails.BillingAddressLine1 = TrimOrNull(dto.BillingDetails.BillingAddressLine1);
            cart.BillingDetails.BillingAddressLine2 = TrimOrNull(dto.BillingDetails.BillingAddressLine2);
            cart.BillingDetails.BillingCity = TrimOrNull(dto.BillingDetails.BillingCity);
            cart.BillingDetails.BillingPostalCode = TrimOrNull(dto.BillingDetails.BillingPostalCode);
            cart.BillingDetails.BillingCountry = TrimOrNull(dto.BillingDetails.BillingCountry);

            ValidateBillingDetails(cart.BillingDetails);
        }

        private static void ValidateReceiptEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            var validator = new EmailAddressAttribute();
            if (!validator.IsValid(email))
                throw new InvalidOperationException("Receipt email address is invalid.");
        }

        private static void ValidateBillingDetails(OrderBillingDetails details)
        {
            ValidateReceiptEmail(details.ReceiptEmail);

            if (details.CustomerType == BillingCustomerType.Company)
            {
                if (string.IsNullOrWhiteSpace(details.CompanyName))
                    throw new InvalidOperationException("Company name is required for invoice.");
                if (string.IsNullOrWhiteSpace(details.TaxId))
                    throw new InvalidOperationException("Tax ID is required for invoice.");
                if (!details.TaxId.All(char.IsDigit))
                    throw new InvalidOperationException("Tax ID must contain only digits.");
                if (string.IsNullOrWhiteSpace(details.BillingAddressLine1)
                    || string.IsNullOrWhiteSpace(details.BillingCity)
                    || string.IsNullOrWhiteSpace(details.BillingPostalCode))
                    throw new InvalidOperationException("Billing address is required for invoice.");
            }
            else if (string.IsNullOrWhiteSpace(details.PersonName))
            {
                throw new InvalidOperationException("Name is required for invoice.");
            }
        }

        private static string? TrimOrNull(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private void AssignDailyRestaurantOrderNumber(Order cart)
        {
            if (!cart.RestaurantId.HasValue)
                return;

            var today = DateOnly.FromDateTime(DateTime.Now);
            var currentMax = _orders.Query()
                .Where(x => x.RestaurantId == cart.RestaurantId.Value && x.DailyRestaurantOrderDate == today)
                .Select(x => x.DailyRestaurantOrderNumber)
                .Max() ?? -1;

            cart.DailyRestaurantOrderDate = today;
            cart.DailyRestaurantOrderNumber = (currentMax + 1) % 10000;
        }

        private static string FormatDisplayOrderNumber(Order order)
            => order.DailyRestaurantOrderNumber.HasValue
                ? order.DailyRestaurantOrderNumber.Value.ToString("0000")
                : order.Id.ToString();

        private static decimal CalculateDeliveryFee(Order cart, decimal subtotal, RestaurantSettings? settings = null)
        {
            if (cart.OrderType != OrderType.Delivery)
                return 0m;

            if (settings is not null && settings.MinimumDeliveryOrder > 0 && subtotal < settings.MinimumDeliveryOrder)
                throw new InvalidOperationException($"Minimum delivery order is {settings.MinimumDeliveryOrder:0.00}.");

            return settings?.DeliveryFee ?? 8.00m;
        }

        private static int EstimatePreparationMinutes(Order cart, RestaurantSettings? settings = null)
        {
            var qty = cart.Items.Sum(i => i.Quantity);
            if (qty == 0) return 0;

            var baseMinutes = settings?.EstimatedPreparationBaseMinutes ?? 10;
            var perItemMinutes = settings?.EstimatedPreparationPerItemMinutes ?? 2;
            return baseMinutes + (perItemMinutes * qty);
        }

        private static void EnsureOrderTypeEnabled(RestaurantSettings settings, OrderType orderType)
        {
            var enabled = orderType switch
            {
                OrderType.Table => settings.EnableTableOrders,
                OrderType.Takeaway => settings.EnableTakeawayOrders,
                OrderType.Delivery => settings.EnableDeliveryOrders,
                _ => false
            };

            if (!enabled)
                throw new InvalidOperationException($"{orderType} orders are not enabled for this restaurant.");
        }

        private static void EnsurePaymentMethodEnabled(RestaurantSettings settings, OrderType orderType, PaymentMethod paymentMethod)
        {
            var enabled = paymentMethod switch
            {
                PaymentMethod.InApp => settings.EnablePayInApp,
                PaymentMethod.AtCounter when orderType == OrderType.Delivery => settings.EnablePayOnDelivery,
                PaymentMethod.AtCounter => settings.EnablePayAtCounter,
                _ => false
            };

            if (!enabled)
                throw new InvalidOperationException($"{paymentMethod} payments are not enabled for this restaurant.");
        }

        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

        private static string CreateCustomizationSignature(CartItemDto item)
            => $"{item.MenuItemId}|{string.Join(",", item.RemovedIngredientIds.OrderBy(x => x))}|{string.Join(",", item.AddedIngredientIds.OrderBy(x => x))}|{item.Note ?? ""}";

        private static OrderItemCustomization ParseCustomization(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new OrderItemCustomization();

            try
            {
                return JsonSerializer.Deserialize<OrderItemCustomization>(value) ?? new OrderItemCustomization();
            }
            catch
            {
                return new OrderItemCustomization();
            }
        }

        private static string? CombineNotes(string? userNote, string? customizationNote)
        {
            var values = new[] { userNote?.Trim(), customizationNote?.Trim() }
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            return values.Count == 0 ? null : string.Join(" | ", values);
        }

        private static string? ExtractUserNote(string? storedNote, string? customizationNote)
        {
            if (string.IsNullOrWhiteSpace(storedNote))
                return null;

            if (string.IsNullOrWhiteSpace(customizationNote))
                return storedNote;

            var suffix = $" | {customizationNote}";
            return storedNote.EndsWith(suffix, StringComparison.Ordinal) ? storedNote[..^suffix.Length] : storedNote;
        }

        private static CustomizationValidationResult ValidateAndBuildCustomization(MenuItemCustomizationSnapshot menuItem, IReadOnlyCollection<int> removedIngredientIds, IReadOnlyCollection<int> addedIngredientIds)
        {
            var defaultIngredients = menuItem.Ingredients.Where(x => x.IsDefault).ToList();
            var removableIngredients = defaultIngredients.Where(x => x.IsRemovable).ToList();
            var substituteIngredients = menuItem.Ingredients.Where(x => x.IsSubstitute).ToList();
            var removableIds = removableIngredients.Select(x => x.IngredientId).ToHashSet();
            var substituteIds = substituteIngredients.Select(x => x.IngredientId).ToHashSet();

            var normalizedRemoved = removedIngredientIds.Where(x => x > 0).Distinct().OrderBy(x => x).ToList();
            var normalizedAdded = addedIngredientIds.Where(x => x > 0).Distinct().OrderBy(x => x).ToList();

            if ((normalizedRemoved.Count > 0 || normalizedAdded.Count > 0) && !menuItem.EnableIngredientSwap)
                throw new InvalidOperationException("Ingredient changes are not enabled for this menu item.");

            if (normalizedRemoved.Any(id => !removableIds.Contains(id)))
                throw new InvalidOperationException("Only swappable dish ingredients can be removed.");

            if (normalizedAdded.Any(id => !substituteIds.Contains(id)))
                throw new InvalidOperationException("Only allowed substitute ingredients can be added.");

            var extraCount = Math.Max(0, normalizedAdded.Count - normalizedRemoved.Count);
            var extraPrice = menuItem.ExtraIngredientPrice;
            var extraCharge = RoundMoney(extraCount * extraPrice);

            var ingredientNames = defaultIngredients
                .Concat(substituteIngredients)
                .Where(x => normalizedRemoved.Contains(x.IngredientId) || normalizedAdded.Contains(x.IngredientId))
                .ToDictionary(
                    x => x.IngredientId,
                    x => x.Name);

            var removedText = normalizedRemoved.Count == 0
                ? null
                : $"Remove: {string.Join(", ", normalizedRemoved.Select(id => ingredientNames[id]))}";
            var addedText = normalizedAdded.Count == 0
                ? null
                : $"Add: {string.Join(", ", normalizedAdded.Select(id => ingredientNames[id]))}";

            var displayNote = string.Join("; ", new[] { removedText, addedText }.Where(x => !string.IsNullOrWhiteSpace(x)));

            var customization = new OrderItemCustomization
            {
                RemovedIngredientIds = normalizedRemoved,
                AddedIngredientIds = normalizedAdded
            };

            return new CustomizationValidationResult
            {
                ExtraCharge = extraCharge,
                Json = (normalizedRemoved.Count == 0 && normalizedAdded.Count == 0) ? null : JsonSerializer.Serialize(customization),
                DisplayNote = string.IsNullOrWhiteSpace(displayNote) ? null : displayNote
            };
        }

        private sealed class CustomizationValidationResult
        {
            public decimal ExtraCharge { get; init; }
            public string? Json { get; init; }
            public string? DisplayNote { get; init; }
        }

        private sealed class MenuItemCustomizationSnapshot
        {
            public int Id { get; init; }
            public bool EnableIngredientSwap { get; init; }
            public decimal ExtraIngredientPrice { get; init; }
            public List<IngredientCustomizationSnapshot> Ingredients { get; init; } = new();
        }

        private sealed class IngredientCustomizationSnapshot
        {
            public int IngredientId { get; init; }
            public bool IsDefault { get; init; }
            public bool IsRemovable { get; init; }
            public bool IsSubstitute { get; init; }
            public string Name { get; init; } = string.Empty;
        }

        private sealed class OrderItemCustomization
        {
            public List<int> RemovedIngredientIds { get; set; } = new();
            public List<int> AddedIngredientIds { get; set; } = new();
        }
    }
}
