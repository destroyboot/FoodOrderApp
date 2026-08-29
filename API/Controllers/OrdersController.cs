using Core.Data.Enums;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly AppDbContext _db;

        public OrdersController(IOrderRepository orders, IAsyncQueryExecutor q, AppDbContext db)
        {
            _orders = orders;
            _q = q;
            _db = db;
        }

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken
            => Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        private string ResolveOwnerKey()
        {
            var key = !string.IsNullOrWhiteSpace(CustomerId) ? CustomerId
                : !string.IsNullOrWhiteSpace(GuestToken) ? GuestToken
                : null;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");

            return key!;
        }

        [HttpGet("my/active")]
        [AllowAnonymous]
        public async Task<IActionResult> MyActive(CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();

            var query = _orders.Query()
                .Where(o => o.CustomerId == ownerKey
                            && o.Status != OrderStatus.Draft
                            && o.Status != OrderStatus.Completed
                            && o.Status != OrderStatus.Cancelled)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.PickupContactName,
                    o.PickupPhone,
                    o.DeliveryContactName,
                    o.DeliveryPhone,
                    o.DeliveryAddressLine1,
                    o.DeliveryCity,
                    o.ScheduledFor,
                    o.Total,
                    o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("my/history")]
        [AllowAnonymous]
        public async Task<IActionResult> MyHistory([FromQuery] int take = 50, CancellationToken ct = default)
        {
            if (take <= 0 || take > 200) take = 50;

            var ownerKey = ResolveOwnerKey();

            var query = _orders.Query()
                .Where(o => o.CustomerId == ownerKey
                            && o.Status != OrderStatus.Draft
                            && (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Cancelled))
                .OrderByDescending(o => o.CreatedAt)
                .Take(take)
                .Select(o => new
                {
                    o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.PickupContactName,
                    o.PickupPhone,
                    o.DeliveryContactName,
                    o.DeliveryPhone,
                    o.DeliveryAddressLine1,
                    o.DeliveryCity,
                    o.ScheduledFor,
                    o.Total,
                    o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> MyOrderDetails(int id, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();

            var query = _orders.Query()
                .Where(o => o.Id == id && o.CustomerId == ownerKey && o.Status != OrderStatus.Draft)
                .Select(o => new
                {
                    o.Id,
                    DisplayOrderNumber = o.DailyRestaurantOrderNumber.HasValue ? o.DailyRestaurantOrderNumber.Value.ToString("0000") : o.Id.ToString(),
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.PickupContactName,
                    o.PickupPhone,
                    o.PickupNote,
                    o.DeliveryContactName,
                    o.DeliveryPhone,
                    o.DeliveryAddressLine1,
                    o.DeliveryAddressLine2,
                    o.DeliveryCity,
                    o.DeliveryPostalCode,
                    o.DeliveryCountry,
                    o.DeliveryNote,
                    o.ReceiptEmail,
                    InvoiceNumber = o.InvoiceDocument != null ? o.InvoiceDocument.InvoiceNumber : null,
                    HasInvoiceDocument = o.InvoiceDocument != null,
                    o.Subtotal,
                    o.DeliveryFee,
                    o.Total,
                    o.EstimatedPreparationMinutes,
                    o.EstimatedReadyAt,
                    o.ScheduledFor,
                    o.CreatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.MenuItemId,
                        MenuItemName = _db.MenuItems
                            .Where(m => m.Id == i.MenuItemId)
                            .Select(m => m.Translations
                                .OrderBy(t => t.Culture == "pl-PL" ? 0 : 1)
                                .Select(t => t.Name)
                                .FirstOrDefault())
                            .FirstOrDefault(),
                        i.Quantity,
                        i.UnitPrice,
                        i.Note
                    }).ToList()
                });

            var result = await _q.FirstOrDefaultAsync(query, ct);
            if (result is null) throw new KeyNotFoundException("Order not found.");

            return Ok(result);
        }

        [HttpGet("{id:int}/invoice-pdf")]
        [AllowAnonymous]
        public async Task<IActionResult> MyInvoicePdf(int id, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();

            var order = await _db.Orders
                .Include(o => o.InvoiceDocument)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == ownerKey && o.Status != OrderStatus.Draft, ct);

            if (order is null)
                throw new KeyNotFoundException("Order not found.");

            if (order.InvoiceDocument is null)
                throw new InvalidOperationException("Invoice is not available for this order.");

            return File(order.InvoiceDocument.PdfBytes, order.InvoiceDocument.ContentType, order.InvoiceDocument.FileName);
        }
    }
}
