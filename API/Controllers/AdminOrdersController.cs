using API.Support;
using Core.Contracts.AdminOrders;
using Core.Contracts.Orders;
using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Persistence;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [Authorize(Roles = "Admin,RestaurantAdmin,Waiter,Chef,DeliveryDriver")]
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IAdminOrderService _admin;
        private readonly INotificationService _notifications;
        private readonly IOrderEventPublisher _events;
        private readonly IOrderStatusEmailService _statusEmails;
        private readonly IEmailSender _email;
        private readonly AppDbContext _db;
        private readonly IOrderSummaryEmailComposer _orderEmails;

        public AdminOrdersController(
            IOrderRepository orders,
            IAsyncQueryExecutor q,
            IAdminOrderService admin,
            INotificationService notifications,
            IOrderEventPublisher events,
            IOrderStatusEmailService statusEmails,
            IEmailSender email,
            AppDbContext db,
            IOrderSummaryEmailComposer orderEmails)
        {
            _orders = orders;
            _q = q;
            _admin = admin;
            _notifications = notifications;
            _events = events;
            _statusEmails = statusEmails;
            _email = email;
            _db = db;
            _orderEmails = orderEmails;
        }

        [HttpGet("active")]
        public async Task<ActionResult<List<AdminOrderListItemDto>>> GetActive(CancellationToken ct)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");
            var allowedRestaurantIds = await GetAllowedRestaurantIdsAsync(ct);
            var isChefOnly = User.IsInRole("Chef")
                && !User.IsInRole("Admin")
                && !User.IsInRole("RestaurantAdmin")
                && !User.IsInRole("Waiter")
                && !User.IsInRole("DeliveryDriver");

            var query = _orders.Query()
                .Where(o => o.Status != OrderStatus.Draft
                            && o.Status != OrderStatus.Completed
                            && o.Status != OrderStatus.Cancelled)
                .Where(o => !isChefOnly || o.Status == OrderStatus.SentToKitchen || o.Status == OrderStatus.Preparing)
                .Where(o => allowedRestaurantIds == null
                            || (o.RestaurantId.HasValue && allowedRestaurantIds.Contains(o.RestaurantId.Value)))
                .Where(o => !User.IsInRole("DeliveryDriver")
                            || User.IsInRole("Admin")
                            || User.IsInRole("RestaurantAdmin")
                            || User.IsInRole("Waiter")
                            || User.IsInRole("Chef")
                            || (o.OrderType == OrderType.Delivery && o.AssignedDeliveryDriverUserId == currentUserId))
                .OrderBy(o => o.CreatedAt)
                .Select(o => new AdminOrderListItemDto
                {
                    Id = o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNumber = o.TableNumber,
                    PickupContactName = o.PickupContactName,
                    PickupPhone = o.PickupPhone,
                    DeliveryContactName = o.DeliveryContactName,
                    DeliveryPhone = o.DeliveryPhone,
                    DeliveryAddressLine1 = o.DeliveryAddressLine1,
                    DeliveryCity = o.DeliveryCity,
                    AssignedDeliveryDriverUserId = o.AssignedDeliveryDriverUserId,
                    AssignedDeliveryDriverName = _db.Users
                        .Where(u => u.Id == o.AssignedDeliveryDriverUserId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    CustomerUserId = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Id)
                        .FirstOrDefault(),
                    CustomerEmail = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    IsAnonymousCustomer = !_db.Users.Any(u => u.Id == o.CustomerId),
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    ReceiptEmail = o.ReceiptEmail,
                    ScheduledFor = o.ScheduledFor,
                    RestaurantId = o.RestaurantId,
                    ReservationId = o.ReservationId,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<AdminOrderListItemDto>>> GetHistory(
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] string? email,
            [FromQuery] OrderStatus? status,
            [FromQuery] OrderType? orderType,
            [FromQuery] int take = 200,
            CancellationToken ct = default)
        {
            if (take <= 0 || take > 500)
                take = 200;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");
            var allowedRestaurantIds = await GetAllowedRestaurantIdsAsync(ct);
            var normalizedEmail = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

            var query = _orders.Query()
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled)
                .Where(o => allowedRestaurantIds == null
                            || (o.RestaurantId.HasValue && allowedRestaurantIds.Contains(o.RestaurantId.Value)))
                .Where(o => !User.IsInRole("DeliveryDriver")
                            || User.IsInRole("Admin")
                            || User.IsInRole("RestaurantAdmin")
                            || User.IsInRole("Waiter")
                            || User.IsInRole("Chef")
                            || (o.OrderType == OrderType.Delivery && o.AssignedDeliveryDriverUserId == currentUserId));

            if (from.HasValue)
                query = query.Where(o => o.CreatedAt >= from.Value);

            if (to.HasValue)
                query = query.Where(o => o.CreatedAt <= to.Value);

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (orderType.HasValue)
                query = query.Where(o => o.OrderType == orderType.Value);

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                query = query.Where(o =>
                    (o.ReceiptEmail != null && o.ReceiptEmail.ToLower().Contains(normalizedEmail))
                    || _db.Users.Any(u => u.Id == o.CustomerId && ((u.Email ?? u.UserName ?? "").ToLower().Contains(normalizedEmail))));
            }

            var projected = query
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .Select(o => new AdminOrderListItemDto
                {
                    Id = o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNumber = o.TableNumber,
                    PickupContactName = o.PickupContactName,
                    PickupPhone = o.PickupPhone,
                    DeliveryContactName = o.DeliveryContactName,
                    DeliveryPhone = o.DeliveryPhone,
                    DeliveryAddressLine1 = o.DeliveryAddressLine1,
                    DeliveryCity = o.DeliveryCity,
                    AssignedDeliveryDriverUserId = o.AssignedDeliveryDriverUserId,
                    AssignedDeliveryDriverName = _db.Users
                        .Where(u => u.Id == o.AssignedDeliveryDriverUserId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    CustomerUserId = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Id)
                        .FirstOrDefault(),
                    CustomerEmail = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    IsAnonymousCustomer = !_db.Users.Any(u => u.Id == o.CustomerId),
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    ReceiptEmail = o.ReceiptEmail,
                    ScheduledFor = o.ScheduledFor,
                    RestaurantId = o.RestaurantId,
                    ReservationId = o.ReservationId,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(projected, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminOrderDetailsDto>> GetById(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var query = _orders.Query()
                .Where(o => o.Id == id)
                .Select(o => new AdminOrderDetailsDto
                {
                    Id = o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNumber = o.TableNumber,
                    PickupContactName = o.PickupContactName,
                    PickupPhone = o.PickupPhone,
                    PickupNote = o.PickupNote,
                    DeliveryContactName = o.DeliveryContactName,
                    DeliveryPhone = o.DeliveryPhone,
                    DeliveryAddressLine1 = o.DeliveryAddressLine1,
                    DeliveryAddressLine2 = o.DeliveryAddressLine2,
                    DeliveryCity = o.DeliveryCity,
                    DeliveryPostalCode = o.DeliveryPostalCode,
                    DeliveryCountry = o.DeliveryCountry,
                    DeliveryNote = o.DeliveryNote,
                    AssignedDeliveryDriverUserId = o.AssignedDeliveryDriverUserId,
                    AssignedDeliveryDriverName = _db.Users
                        .Where(u => u.Id == o.AssignedDeliveryDriverUserId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    CustomerUserId = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Id)
                        .FirstOrDefault(),
                    CustomerEmail = _db.Users
                        .Where(u => u.Id == o.CustomerId)
                        .Select(u => u.Email ?? u.UserName)
                        .FirstOrDefault(),
                    IsAnonymousCustomer = !_db.Users.Any(u => u.Id == o.CustomerId),
                    ScheduledFor = o.ScheduledFor,
                    RestaurantId = o.RestaurantId,
                    ReservationId = o.ReservationId,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    ReceiptEmail = o.ReceiptEmail,
                    ReceiptSentAt = o.ReceiptSentAt,
                    InvoiceNumber = o.InvoiceDocument != null ? o.InvoiceDocument.InvoiceNumber : null,
                    HasInvoiceDocument = o.InvoiceDocument != null,
                    Subtotal = o.Subtotal,
                    DeliveryFee = o.DeliveryFee,
                    Total = o.Total,
                    EstimatedPreparationMinutes = o.EstimatedPreparationMinutes,
                    EstimatedReadyAt = o.EstimatedReadyAt,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new AdminOrderLineDto
                    {
                        MenuItemId = i.MenuItemId,
                        MenuItemName = _db.MenuItems
                            .Where(m => m.Id == i.MenuItemId)
                            .Select(m => m.Translations
                                .OrderBy(t => t.Culture == "pl-PL" ? 0 : 1)
                                .Select(t => t.Name)
                                .FirstOrDefault())
                            .FirstOrDefault(),
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Note = i.Note
                    }).ToList(),
                    BillingDetails = o.BillingDetails == null ? null : new OrderBillingDetailsDto
                    {
                        CustomerType = o.BillingDetails.CustomerType,
                        InvoiceStatus = o.BillingDetails.InvoiceStatus,
                        ReceiptEmail = o.BillingDetails.ReceiptEmail,
                        PersonName = o.BillingDetails.PersonName,
                        CompanyName = o.BillingDetails.CompanyName,
                        TaxId = o.BillingDetails.TaxId,
                        BillingAddressLine1 = o.BillingDetails.BillingAddressLine1,
                        BillingAddressLine2 = o.BillingDetails.BillingAddressLine2,
                        BillingCity = o.BillingDetails.BillingCity,
                        BillingPostalCode = o.BillingDetails.BillingPostalCode,
                        BillingCountry = o.BillingDetails.BillingCountry,
                        InvoiceIssuedAt = o.BillingDetails.InvoiceIssuedAt,
                        InvoiceSentAt = o.BillingDetails.InvoiceSentAt
                    },
                    Comments = o.Comments
                        .OrderBy(c => c.CreatedAt)
                        .Select(c => new OrderCommentDto
                        {
                            Id = c.Id,
                            OrderId = c.OrderId,
                            AuthorUserId = c.AuthorUserId,
                            AuthorRole = c.AuthorRole,
                            Body = c.Body,
                            IsCustomerVisible = c.IsCustomerVisible,
                            CreatedAt = c.CreatedAt
                        }).ToList()
                });

            var result = await _q.FirstOrDefaultAsync(query, ct);
            if (result is null) return NotFound();

            return Ok(result);
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] OrderStatus newStatus, CancellationToken ct)
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            await EnsureCanAccessOrderAsync(id, ct);
            await _admin.ChangeStatusAsync(id, newStatus, userId, roles, ct);
            return NoContent();
        }

        [HttpPatch("{id:int}/mark-paid")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter,DeliveryDriver")]
        public async Task<IActionResult> MarkPaid(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return NotFound();

            if (order.PaymentMethod != PaymentMethod.AtCounter)
                throw new InvalidOperationException("Only pay-at-counter orders can be marked as paid.");

            if (order.Status is OrderStatus.Draft or OrderStatus.Cancelled)
                throw new InvalidOperationException("This order cannot be marked as paid.");

            order.PaymentStatus = PaymentStatus.Paid;
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{id:int}/delivery-drivers")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<ActionResult<List<DeliveryDriverOptionDto>>> GetDeliveryDrivers(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var restaurantId = await _orders.Query()
                .Where(o => o.Id == id)
                .Select(o => o.RestaurantId)
                .FirstOrDefaultAsync(ct);

            if (!restaurantId.HasValue)
                throw new InvalidOperationException("Order has no restaurant assigned.");

            var drivers = await (
                from assignment in _db.RestaurantUserRoles
                join user in _db.Users on assignment.UserId equals user.Id
                where assignment.RestaurantId == restaurantId.Value && assignment.Role == "DeliveryDriver"
                orderby user.Email, user.UserName
                select new DeliveryDriverOptionDto
                {
                    UserId = user.Id,
                    DisplayName = user.Email ?? user.UserName ?? user.Id,
                    Email = user.Email
                })
                .ToListAsync(ct);

            return Ok(drivers);
        }

        [HttpPatch("{id:int}/assign-delivery-driver")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<IActionResult> AssignDeliveryDriver(int id, [FromQuery] string? userId, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return NotFound();

            if (order.OrderType != OrderType.Delivery)
                throw new InvalidOperationException("Only delivery orders can be assigned to a delivery driver.");

            if (string.IsNullOrWhiteSpace(userId))
            {
                order.AssignedDeliveryDriverUserId = null;
                await _db.SaveChangesAsync(ct);
                return NoContent();
            }

            var trimmedUserId = userId.Trim();
            var wasAssignedTo = order.AssignedDeliveryDriverUserId;
            var driverExists = await (
                from assignment in _db.RestaurantUserRoles
                join user in _db.Users on assignment.UserId equals user.Id
                where assignment.RestaurantId == order.RestaurantId
                      && assignment.Role == "DeliveryDriver"
                      && assignment.UserId == trimmedUserId
                select user.Id)
                .AnyAsync(ct);

            if (!driverExists)
                throw new InvalidOperationException("Selected delivery driver is not assigned to this restaurant.");

            order.AssignedDeliveryDriverUserId = trimmedUserId;
            await _db.SaveChangesAsync(ct);

            if (!string.Equals(wasAssignedTo, trimmedUserId, StringComparison.Ordinal))
            {
                var driverName = await _db.Users
                    .Where(u => u.Id == trimmedUserId)
                    .Select(u => u.Email ?? u.UserName ?? u.Id)
                    .FirstAsync(ct);

                var restaurantName = await _db.Restaurants
                    .Where(r => r.Id == order.RestaurantId)
                    .Select(r => r.Name)
                    .FirstOrDefaultAsync(ct);

                await _notifications.CreateAsync(
                    ownerKey: trimmedUserId,
                    type: NotificationType.DeliveryAssigned,
                    title: "New delivery assigned",
                    body: $"Order #{FormatDisplayOrderNumber(order)} was assigned to you{(string.IsNullOrWhiteSpace(restaurantName) ? "" : $" for {restaurantName}")}.",
                    payloadJson: JsonSerializer.Serialize(new
                    {
                        type = "delivery-assigned",
                        orderId = order.Id,
                        url = $"/orders/{order.Id}"
                    }),
                    ct: ct);
            }
            return NoContent();
        }

        [HttpPatch("{id:int}/collect-payment-and-complete")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter,DeliveryDriver")]
        public async Task<IActionResult> CollectPaymentAndComplete(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders
                .Include(x => x.BillingDetails)
                .Include(x => x.InvoiceDocument)
                .Include(x => x.Items)
                .Include(x => x.Restaurant)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return NotFound();

            if (order.OrderType != OrderType.Delivery)
                throw new InvalidOperationException("Only delivery orders can be completed by delivery collection.");

            if (order.PaymentMethod != PaymentMethod.AtCounter)
                throw new InvalidOperationException("Only pay-on-delivery orders can be marked as collected.");

            if (order.Status is OrderStatus.Draft or OrderStatus.Cancelled)
                throw new InvalidOperationException("This order cannot be completed.");

            var oldStatus = order.Status;
            order.PaymentStatus = PaymentStatus.Paid;
            order.Status = OrderStatus.Completed;
            order.DeliveryState = DeliveryState.Delivered;

            await _db.SaveChangesAsync(ct);

            await _notifications.CreateAsync(
                ownerKey: order.CustomerId!,
                type: NotificationType.OrderStatusChanged,
                title: "Order status updated",
                body: $"Order #{FormatDisplayOrderNumber(order)}: {oldStatus} -> Completed",
                payloadJson: JsonSerializer.Serialize(new
                {
                    type = "order-status-changed",
                    orderId = order.Id,
                    oldStatus = oldStatus.ToString(),
                    newStatus = order.Status.ToString(),
                    url = "/orders"
                }),
                ct: ct);

            await _statusEmails.TrySendStatusChangedEmailAsync(
                ownerKey: order.CustomerId!,
                orderId: order.Id,
                oldStatus: oldStatus,
                newStatus: OrderStatus.Completed,
                ct: ct);

            await _events.PublishOrderStatusChangedAsync(order.Id, oldStatus, OrderStatus.Completed, ct);
            return NoContent();
        }

        [HttpPost("{id:int}/comments")]
        public async Task<ActionResult<OrderCommentDto>> AddComment(int id, OrderCommentCreateDto dto, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var body = dto.Body?.Trim();
            if (string.IsNullOrWhiteSpace(body))
                throw new InvalidOperationException("Comment body is required.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindAll(ClaimTypes.Role).Select(c => c.Value).FirstOrDefault();
            var comment = new OrderComment
            {
                OrderId = id,
                AuthorUserId = userId,
                AuthorRole = role,
                Body = body,
                IsCustomerVisible = dto.IsCustomerVisible
            };

            _db.OrderComments.Add(comment);
            await _db.SaveChangesAsync(ct);

            return Ok(new OrderCommentDto
            {
                Id = comment.Id,
                OrderId = comment.OrderId,
                AuthorUserId = comment.AuthorUserId,
                AuthorRole = comment.AuthorRole,
                Body = comment.Body,
                IsCustomerVisible = comment.IsCustomerVisible,
                CreatedAt = comment.CreatedAt
            });
        }

        [HttpPatch("{id:int}/invoice-status")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<IActionResult> UpdateInvoiceStatus(int id, [FromQuery] InvoiceStatus status, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders
                .Include(x => x.BillingDetails)
                .Include(x => x.InvoiceDocument)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return NotFound();

            order.BillingDetails ??= new OrderBillingDetails { OrderId = id };
            order.BillingDetails.InvoiceStatus = status;

            if (status is InvoiceStatus.Issued or InvoiceStatus.Sent)
            {
                await ResolveReceiptEmailAsync(order, ct);
                ValidateInvoiceDetails(order);
            }

            if (status == InvoiceStatus.Issued)
            {
                await EnsureInvoiceDocumentAsync(order, ct);
                order.BillingDetails.InvoiceIssuedAt = DateTime.UtcNow;
            }
            if (status == InvoiceStatus.Sent)
            {
                var invoiceDocument = await EnsureInvoiceDocumentAsync(order, ct);
                order.BillingDetails.InvoiceSentAt = DateTime.UtcNow;
                order.ReceiptSentAt = DateTime.UtcNow;

                var toEmail = await ResolveReceiptEmailAsync(order, ct);
                if (string.IsNullOrWhiteSpace(toEmail))
                    throw new InvalidOperationException("No receipt email is available for this order.");

                var emailModel = await _orderEmails.ComposeAsync(order, invoiceDocument, ct);
                await _email.SendAsync(
                    toEmail,
                    emailModel.Subject,
                    OrderSummaryEmailBuilder.Build(emailModel),
                    new[]
                    {
                        new EmailAttachment
                        {
                            FileName = invoiceDocument.FileName,
                            ContentType = invoiceDocument.ContentType,
                            ContentBytes = invoiceDocument.PdfBytes
                        }
                    },
                    ct);
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpPost("{id:int}/send-receipt")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<IActionResult> SendReceipt(int id, [FromBody] SendReceiptRequestDto? req, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders
                .Include(o => o.BillingDetails)
                .Include(o => o.InvoiceDocument)
                .Include(o => o.Items)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == id && o.Status != OrderStatus.Draft, ct);

            if (order is null) return NotFound();

            var toEmail = string.IsNullOrWhiteSpace(req?.Email)
                ? await ResolveReceiptEmailAsync(order, ct)
                : req!.Email!.Trim();
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("No receipt email is available for this order.");

            var invoiceDocument = order.BillingDetails?.InvoiceStatus is InvoiceStatus.Requested or InvoiceStatus.Issued or InvoiceStatus.Sent
                ? await EnsureInvoiceDocumentAsync(order, ct)
                : order.InvoiceDocument;
            var emailModel = await _orderEmails.ComposeAsync(order, invoiceDocument, ct);
            var attachments = invoiceDocument is null
                ? null
                : new[]
                {
                    new EmailAttachment
                    {
                        FileName = invoiceDocument.FileName,
                        ContentType = invoiceDocument.ContentType,
                        ContentBytes = invoiceDocument.PdfBytes
                    }
                };
            await _email.SendAsync(
                toEmail,
                emailModel.Subject,
                OrderSummaryEmailBuilder.Build(emailModel),
                attachments,
                ct);

            order.ReceiptSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { sentTo = toEmail });
        }

        [HttpGet("{id:int}/invoice-pdf")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<IActionResult> DownloadInvoicePdf(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders
                .Include(o => o.BillingDetails)
                .Include(o => o.InvoiceDocument)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null) return NotFound();

            var invoiceDocument = await EnsureInvoiceDocumentAsync(order, ct);
            return File(invoiceDocument.PdfBytes, invoiceDocument.ContentType, invoiceDocument.FileName);
        }

        [HttpGet("{id:int}/summary-pdf")]
        [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
        public async Task<IActionResult> DownloadSummaryPdf(int id, CancellationToken ct)
        {
            await EnsureCanAccessOrderAsync(id, ct);

            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.Restaurant)
                .FirstOrDefaultAsync(o => o.Id == id, ct);

            if (order is null)
                return NotFound();

            var fileName = $"order-summary-{FormatDisplayOrderNumber(order)}.pdf";
            var content = OrderSummaryPdfBuilder.Build(new
            {
                DisplayOrderNumber = FormatDisplayOrderNumber(order),
                RestaurantName = order.Restaurant?.Name ?? "-",
                CreatedAt = order.CreatedAt,
                Status = order.Status.ToString(),
                OrderType = order.OrderType.ToString(),
                PaymentMethod = order.PaymentMethod.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                order.Subtotal,
                order.DeliveryFee,
                order.Total,
                Items = order.Items.Select(i => new
                {
                    Name = _db.MenuItems
                        .Where(m => m.Id == i.MenuItemId)
                        .Select(m => m.Translations
                            .OrderBy(t => t.Culture == "pl-PL" ? 0 : 1)
                            .Select(t => t.Name)
                            .FirstOrDefault())
                        .FirstOrDefault() ?? $"#{i.MenuItemId}",
                    i.Quantity,
                    i.UnitPrice,
                    i.Note,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList()
            });

            return File(content, "application/pdf", fileName);
        }

        private async Task<List<int>?> GetAllowedRestaurantIdsAsync(CancellationToken ct)
        {
            if (User.IsInRole("Admin"))
                return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");

            return await _db.RestaurantUserRoles
                .Where(x => x.UserId == userId)
                .Select(x => x.RestaurantId)
                .Distinct()
                .ToListAsync(ct);
        }

        private async Task EnsureCanAccessOrderAsync(int orderId, CancellationToken ct)
        {
            if (User.IsInRole("Admin"))
                return;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");

            if (User.IsInRole("DeliveryDriver")
                && !User.IsInRole("RestaurantAdmin")
                && !User.IsInRole("Waiter")
                && !User.IsInRole("Chef"))
            {
                var isAssignedDelivery = await _orders.Query()
                    .AnyAsync(o => o.Id == orderId
                                   && o.OrderType == OrderType.Delivery
                                   && o.AssignedDeliveryDriverUserId == userId,
                        ct);

                if (!isAssignedDelivery)
                    throw new InvalidOperationException("You are not allowed to access this delivery.");

                return;
            }

            var restaurantId = await _orders.Query()
                .Where(o => o.Id == orderId)
                .Select(o => o.RestaurantId)
                .FirstOrDefaultAsync(ct);

            if (!restaurantId.HasValue)
                throw new InvalidOperationException("Order has no restaurant assigned.");

            var canAccess = await _db.RestaurantUserRoles.AnyAsync(
                x => x.RestaurantId == restaurantId.Value && x.UserId == userId,
                ct);

            if (!canAccess)
                throw new InvalidOperationException("You are not allowed to access this order.");
        }

        private async Task<OrderInvoiceDocument> EnsureInvoiceDocumentAsync(Order order, CancellationToken ct)
        {
            order.BillingDetails ??= new OrderBillingDetails { OrderId = order.Id };

            if (order.InvoiceDocument is not null)
                return order.InvoiceDocument;

            var existing = await _db.OrderInvoiceDocuments.FirstOrDefaultAsync(x => x.OrderId == order.Id, ct);
            if (existing is not null)
            {
                order.InvoiceDocument = existing;
                return existing;
            }

            var generatedAt = DateTime.UtcNow;
            var invoiceNumber = $"INV-{generatedAt:yyyyMMdd}-{order.Id}";
            var document = new OrderInvoiceDocument
            {
                OrderId = order.Id,
                InvoiceNumber = invoiceNumber,
                FileName = $"{invoiceNumber}.pdf",
                ContentType = "application/pdf",
                GeneratedAt = generatedAt,
                PdfBytes = InvoicePdfBuilder.Build(CreateInvoicePdfModel(order, invoiceNumber))
            };

            _db.OrderInvoiceDocuments.Add(document);
            order.InvoiceDocument = document;
            await _db.SaveChangesAsync(ct);
            return document;
        }

        private static object CreateInvoicePdfModel(Order order, string invoiceNumber)
        {
            var customerName = order.BillingDetails?.CustomerType == BillingCustomerType.Company
                ? order.BillingDetails?.CompanyName
                : order.BillingDetails?.PersonName;
            var address = string.Join(", ", new[]
            {
                order.BillingDetails?.BillingAddressLine1,
                order.BillingDetails?.BillingAddressLine2,
                order.BillingDetails?.BillingCity,
                order.BillingDetails?.BillingPostalCode,
                order.BillingDetails?.BillingCountry
            }.Where(x => !string.IsNullOrWhiteSpace(x)));

            return new
            {
                InvoiceNumber = invoiceNumber,
                OrderId = order.Id,
                CreatedAt = order.CreatedAt,
                CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Customer" : customerName,
                Address = string.IsNullOrWhiteSpace(address) ? "-" : address,
                TaxId = order.BillingDetails?.TaxId ?? "-",
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                Total = order.Total,
                Items = order.Items.Select(i => new
                {
                    i.MenuItemId,
                    i.Quantity,
                    i.UnitPrice,
                    i.Note,
                    LineTotal = i.UnitPrice * i.Quantity
                }).ToList()
            };
        }

        private async Task<object> CreateOrderEmailModelAsync(Order order, OrderInvoiceDocument? invoiceDocument, CancellationToken ct)
        {
            var itemNames = await LoadMenuItemNamesAsync(order, ct);
            return new
            {
                order.Id,
                DisplayOrderNumber = FormatDisplayOrderNumber(order),
                order.Status,
                order.OrderType,
                order.TableNumber,
                RestaurantName = order.Restaurant?.Name,
                order.Subtotal,
                order.DeliveryFee,
                order.Total,
                order.CreatedAt,
                InvoiceNumber = invoiceDocument?.InvoiceNumber,
                Items = order.Items.Select(i => new
                {
                    i.MenuItemId,
                    Name = itemNames.GetValueOrDefault(i.MenuItemId, $"Menu item #{i.MenuItemId}"),
                    i.Quantity,
                    i.UnitPrice,
                    i.Note
                }).ToList()
            };
        }

        private async Task<Dictionary<int, string>> LoadMenuItemNamesAsync(Order order, CancellationToken ct)
        {
            var ids = order.Items.Select(item => item.MenuItemId).Distinct().ToArray();
            return await _db.MenuItems
                .Where(menuItem => ids.Contains(menuItem.Id))
                .Select(menuItem => new
                {
                    menuItem.Id,
                    Name = menuItem.Translations
                        .OrderBy(translation => translation.Culture == "pl-PL" ? 0 : 1)
                        .Select(translation => translation.Name)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(item => item.Id, item => item.Name ?? $"Menu item #{item.Id}", ct);
        }

        private static void ValidateInvoiceDetails(Order order)
        {
            var details = order.BillingDetails;
            if (details is null)
                throw new InvalidOperationException("Invoice details are required before issuing an invoice.");

            var email = (order.ReceiptEmail ?? details.ReceiptEmail)?.Trim();
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Receipt email is required before issuing an invoice.");

            if (details.CustomerType == BillingCustomerType.Company)
            {
                if (string.IsNullOrWhiteSpace(details.CompanyName))
                    throw new InvalidOperationException("Company name is required before issuing an invoice.");
                if (string.IsNullOrWhiteSpace(details.TaxId) || !details.TaxId.All(char.IsDigit))
                    throw new InvalidOperationException("A numeric Tax ID is required before issuing an invoice.");
            }
            else if (string.IsNullOrWhiteSpace(details.PersonName))
            {
                throw new InvalidOperationException("Customer name is required before issuing an invoice.");
            }

            if (string.IsNullOrWhiteSpace(details.BillingAddressLine1)
                || string.IsNullOrWhiteSpace(details.BillingCity)
                || string.IsNullOrWhiteSpace(details.BillingPostalCode))
            {
                throw new InvalidOperationException("Billing address line 1, city, and postal code are required before issuing an invoice.");
            }
        }

        private async Task<string?> ResolveReceiptEmailAsync(Order order, CancellationToken ct)
        {
            var toEmail = (order.ReceiptEmail ?? order.BillingDetails?.ReceiptEmail)?.Trim();
            if (!string.IsNullOrWhiteSpace(toEmail))
                return toEmail;

            if (string.IsNullOrWhiteSpace(order.CustomerId))
                return null;

            var accountEmail = await _db.Users
                .Where(u => u.Id == order.CustomerId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);

            if (string.IsNullOrWhiteSpace(accountEmail))
                return null;

            order.ReceiptEmail = accountEmail.Trim();
            return order.ReceiptEmail;
        }

        private static string FormatDisplayOrderNumber(Order order)
            => order.DailyRestaurantOrderNumber.HasValue
                ? order.DailyRestaurantOrderNumber.Value.ToString("0000")
                : order.Id.ToString();
    }
}
