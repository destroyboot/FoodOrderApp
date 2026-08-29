using Core.Contracts.Push;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/push/devices")]
    public class PushDevicesController : ControllerBase
    {
        private readonly IPushNotificationService _pushNotifications;

        public PushDevicesController(IPushNotificationService pushNotifications)
        {
            _pushNotifications = pushNotifications;
        }

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken =>
            Request.Headers.TryGetValue("X-Guest-Token", out var value) ? value.ToString() : null;

        private string ResolveOwnerKey()
        {
            var key = !string.IsNullOrWhiteSpace(CustomerId) ? CustomerId
                : !string.IsNullOrWhiteSpace(GuestToken) ? GuestToken
                : null;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");

            return key!;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] PushDeviceRegisterDto dto, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();
            await _pushNotifications.RegisterDeviceAsync(ownerKey, dto.ExpoPushToken, dto.Platform, dto.DeviceName, ct);
            return NoContent();
        }

        [HttpDelete("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Unregister([FromBody] PushDeviceUnregisterDto dto, CancellationToken ct)
        {
            var ownerKey = ResolveOwnerKey();
            await _pushNotifications.UnregisterDeviceAsync(ownerKey, dto.ExpoPushToken, ct);
            return NoContent();
        }
    }
}
