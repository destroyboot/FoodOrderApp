using Core.Contracts.AdminOrders;
using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

namespace API.Controllers
{
    [Authorize(Roles = "Admin,Waiter,Chef")]
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IAdminOrderService _admin;

        public AdminOrdersController(IOrderRepository orders, IAsyncQueryExecutor q, IAdminOrderService admin)
        {
            _orders = orders;
            _q = q;
            _admin = admin;
        }

        // Active = not Draft, not Served, not Cancelled
        [HttpGet("active")]
        public async Task<ActionResult<List<AdminOrderListItemDto>>> GetActive(CancellationToken ct)
        {
            var query = _orders.Query()
                .Where(o => o.Status != OrderStatus.Draft
                            && o.Status != OrderStatus.Completed
                            && o.Status != OrderStatus.Cancelled)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new AdminOrderListItemDto
                {
                    Id = o.Id,
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNumber = o.TableNumber,
                    Total = o.Total,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.Items.Sum(i => i.Quantity)
                });

            var result = await _q.ToListAsync(query, ct);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AdminOrderDetailsDto>> GetById(int id, CancellationToken ct)
        {
            var query = _orders.Query()
                .Where(o => o.Id == id)
                .Select(o => new AdminOrderDetailsDto
                {
                    Id = o.Id,
                    Status = o.Status,
                    OrderType = o.OrderType,
                    TableNumber = o.TableNumber,
                    Subtotal = o.Subtotal,
                    DeliveryFee = o.DeliveryFee,
                    Total = o.Total,
                    EstimatedPreparationMinutes = o.EstimatedPreparationMinutes,
                    EstimatedReadyAt = o.EstimatedReadyAt,
                    CreatedAt = o.CreatedAt,
                    Items = o.Items.Select(i => new AdminOrderLineDto
                    {
                        MenuItemId = i.MenuItemId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Note = i.Note
                    }).ToList()
                });

            var result = await _q.FirstOrDefaultAsync(query, ct);
            if (result is null) return NotFound();

            return Ok(result);
        }

        // Status change
        // Example: PATCH /api/admin/orders/123/status?newStatus=Accepted
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] OrderStatus newStatus, CancellationToken ct)
        {
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            await _admin.ChangeStatusAsync(id, newStatus, userId, roles, ct);
            return NoContent();
        }
    }
}
