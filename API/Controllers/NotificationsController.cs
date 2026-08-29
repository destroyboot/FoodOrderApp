using Azure.Core;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private readonly IAsyncQueryExecutor _q;
        private readonly Core.Interfaces.INotificationRepository _repo;
        private readonly INotificationService _service;

        public NotificationsController(
            Core.Interfaces.INotificationRepository repo,
            INotificationService service,
            IAsyncQueryExecutor q)
        {
            _repo = repo;
            _service = service;
            _q = q;
        }

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken =>
            Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        private string ResolveOwnerKey()
        {
            var key = !string.IsNullOrWhiteSpace(CustomerId) ? CustomerId
                : !string.IsNullOrWhiteSpace(GuestToken) ? GuestToken
                : null;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");

            return key!;
        }

        // List newest first
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get([FromQuery] int take = 50, CancellationToken ct = default)
        {
            if (take <= 0 || take > 200) take = 50;
            var ownerKey = ResolveOwnerKey();

            var query = _repo.Query()
                .Where(n => n.OwnerKey == ownerKey)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Body,
                    n.PayloadJson,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReadAt
                });

            return Ok(await _q.ToListAsync(query, ct));
        }

        [HttpPatch("{id:int}/read")]
        [AllowAnonymous]
        public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();
            await _service.MarkReadAsync(ownerKey, id, ct);
            return NoContent();
        }

        [HttpPatch("read-all")]
        [AllowAnonymous]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();
            await _service.MarkAllReadAsync(ownerKey, ct);
            return NoContent();
        }
    }
}
