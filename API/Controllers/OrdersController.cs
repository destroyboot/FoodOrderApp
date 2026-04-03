using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;

        public OrdersController(IOrderRepository orders, IAsyncQueryExecutor q)
        {
            _orders = orders;
            _q = q;
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

        // Active orders for current user/guest
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
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.Total,
                    o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        // Order history for current user/guest
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
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.Total,
                    o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        // Order details (owner only)
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
                    o.Status,
                    o.OrderType,
                    o.TableNumber,
                    o.Subtotal,
                    o.DeliveryFee,
                    o.Total,
                    o.EstimatedPreparationMinutes,
                    o.EstimatedReadyAt,
                    o.CreatedAt,
                    Items = o.Items.Select(i => new
                    {
                        i.MenuItemId,
                        i.Quantity,
                        i.UnitPrice,
                        i.Note
                    }).ToList()
                });

            var result = await _q.FirstOrDefaultAsync(query, ct);
            if (result is null) throw new KeyNotFoundException("Order not found.");

            return Ok(result);
        }
    }
}
