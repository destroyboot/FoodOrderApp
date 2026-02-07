using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/maintenance")]
    [Authorize(Roles = "Admin")]
    public class AdminMaintenanceController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;

        public AdminMaintenanceController(UserManager<ApplicationUser> users)
        {
            _users = users;
        }

        [HttpPost("purge-expired-pending-users")]
        public async Task<IActionResult> PurgeExpiredPendingUsers(CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // users that are not confirmed and their registration code expired
            var expired = _users.Users
                .Where(u => !u.EmailConfirmed
                            && u.RegistrationCodeExpiresAt != null
                            && u.RegistrationCodeExpiresAt <= now);

            var list = expired.ToList();
            var deleted = 0;

            foreach (var u in list)
            {
                var res = await _users.DeleteAsync(u);
                if (res.Succeeded) deleted++;
            }

            return Ok(new { found = list.Count, deleted });
        }
    }
}
