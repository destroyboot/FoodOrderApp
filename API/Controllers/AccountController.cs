using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class AccountController : ControllerBase
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;

        public AccountController(IOrderRepository orders, IAsyncQueryExecutor q, IUnitOfWork uow)
        {
            _orders = orders;
            _q = q;
            _uow = uow;
        }

        private string UserId =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id.");

        private string? GuestToken =>
            Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        /// <summary>
        /// Claims guest orders (and the draft cart) into the authenticated user's account.
        /// Send X-Guest-Token header + Bearer JWT.
        /// </summary>
        [Authorize] // any logged-in user
        [HttpPost("claim")]
        public async Task<IActionResult> ClaimGuestOrders(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(GuestToken))
                throw new InvalidOperationException("Provide X-Guest-Token header.");

            var guest = GuestToken!.Trim();
            var userId = UserId;

            // If someone accidentally tries to "claim" with token equal to userId, just do nothing
            if (string.Equals(guest, userId, StringComparison.Ordinal))
                return Ok(new { movedOrders = 0 });

            // Load tracked orders owned by the guest token and reassign them
            var guestOrdersQuery = _orders.Query(tracked: true)
                .Where(o => o.CustomerId == guest);

            var guestOrders = await _q.ToListAsync(guestOrdersQuery, ct);

            // If there are no guest orders, nothing to do
            if (guestOrders.Count == 0)
                return Ok(new { movedOrders = 0 });

            // If user already has a Draft cart, we should NOT move a guest Draft cart over.
            // Rule: one Draft per user.
            var userHasDraft = await _q.AnyAsync(
                _orders.Query().Where(o => o.CustomerId == userId && o.Status == OrderStatus.Draft),
                ct);

            var moved = 0;

            foreach (var o in guestOrders)
            {
                if (o.Status == OrderStatus.Draft && userHasDraft)
                {
                    // Option: keep guest draft as-is, or delete it. We'll keep it.
                    continue;
                }

                o.CustomerId = userId;
                moved++;
            }

            await _uow.SaveChangesAsync(ct);

            return Ok(new { movedOrders = moved });
        }
    }
}
