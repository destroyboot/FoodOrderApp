using Core.Contracts.Users;
using Core.Data.Entities;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private static readonly string[] RestaurantScopedRoles = ["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"];

        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;
        private readonly IEmailSender _email;

        public AdminUsersController(
            UserManager<ApplicationUser> users,
            RoleManager<IdentityRole> roles,
            AppDbContext db,
            IConfiguration cfg,
            IEmailSender email)
        {
            _users = users;
            _roles = roles;
            _db = db;
            _cfg = cfg;
            _email = email;
        }

        private string CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id.");

        [HttpGet("available-roles")]
        public IActionResult GetAvailableRoles()
        {
            var roles = _roles.Roles
                .Select(r => r.Name!)
                .Where(x => !RestaurantScopedRoles.Contains(x))
                .OrderBy(x => x)
                .ToList();

            return Ok(roles);
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(CancellationToken ct)
        {
            var users = await _users.Users
                .OrderBy(u => u.Email)
                .ToListAsync(ct);

            var result = new List<UserDto>();
            foreach (var user in users)
            {
                result.Add(await BuildUserDtoAsync(user, ct));
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            return Ok(await BuildUserDtoAsync(user, ct));
        }

        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] AdminUserCreateDto req, CancellationToken ct)
        {
            await EnsureEmailAndUsernameAvailableAsync(req.Email, req.UserName, null, ct);

            var user = new ApplicationUser
            {
                Email = NormalizeEmail(req.Email),
                UserName = NormalizeUserName(req.UserName),
                EmailConfirmed = req.EmailConfirmed
            };

            var create = await _users.CreateAsync(user, req.Password);
            ThrowIfFailed(create);

            await SyncGlobalRolesAsync(user, req.IsAppAdmin, ct);
            await SyncRestaurantAssignmentsAsync(user, req.RestaurantAssignments, ct);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, await BuildUserDtoAsync(user, ct));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUserUpdateDto req, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            if (id == CurrentUserId && !req.IsAppAdmin)
                throw new InvalidOperationException("You cannot remove your own Admin access.");

            await EnsureEmailAndUsernameAvailableAsync(req.Email, req.UserName, id, ct);

            user.Email = NormalizeEmail(req.Email);
            user.UserName = NormalizeUserName(req.UserName);
            user.EmailConfirmed = req.EmailConfirmed;

            var update = await _users.UpdateAsync(user);
            ThrowIfFailed(update);

            await SyncGlobalRolesAsync(user, req.IsAppAdmin, ct);
            await SyncRestaurantAssignmentsAsync(user, req.RestaurantAssignments, ct);

            return Ok(await BuildUserDtoAsync(user, ct));
        }

        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(string id, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var email = NormalizeEmail(user.Email ?? throw new InvalidOperationException("User email is required."));
            var encodedToken = System.Net.WebUtility.UrlEncode(token);
            var encodedEmail = System.Net.WebUtility.UrlEncode(email);
            var frontendBaseUrl = _cfg["Frontend:BaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:5173";
            var resetLink = $"{frontendBaseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            await _email.SendAsync(
                toEmail: email,
                subject: "Food Order App - Password reset",
                htmlBody: $"""
                    <p>Hello,</p>
                    <p>An administrator requested a password reset for your account.</p>
                    <p><a href="{resetLink}">Reset your password</a></p>
                    <p>If you did not expect this email, you can ignore it.</p>
                    """,
                ct: ct);

            return NoContent();
        }

        [HttpPost("assignments/bulk")]
        public async Task<IActionResult> AddBulkRestaurantAssignment([FromBody] AdminUserBulkRestaurantAssignmentDto req, CancellationToken ct)
        {
            if (req.RestaurantId <= 0)
                throw new InvalidOperationException("Restaurant is required.");

            var role = NormalizeRestaurantRole(req.Role);
            var userIds = req.UserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (userIds.Count == 0)
                throw new InvalidOperationException("At least one user must be selected.");

            var restaurantExists = await _db.Restaurants.AnyAsync(x => x.Id == req.RestaurantId, ct);
            if (!restaurantExists)
                throw new InvalidOperationException("Restaurant not found.");

            var users = await _users.Users
                .Where(x => userIds.Contains(x.Id))
                .ToListAsync(ct);

            var foundIds = users.Select(x => x.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingIds = userIds.Where(x => !foundIds.Contains(x)).ToList();
            if (missingIds.Count > 0)
                throw new InvalidOperationException($"Users not found: {string.Join(", ", missingIds)}");

            var existingAssignments = await _db.RestaurantUserRoles
                .Where(x => x.RestaurantId == req.RestaurantId && x.Role == role && userIds.Contains(x.UserId))
                .ToListAsync(ct);

            var existingUserIds = existingAssignments
                .Select(x => x.UserId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var user in users)
            {
                if (!existingUserIds.Contains(user.Id))
                {
                    _db.RestaurantUserRoles.Add(new RestaurantUserRole
                    {
                        RestaurantId = req.RestaurantId,
                        UserId = user.Id,
                        Role = role
                    });
                }

                if (!await _users.IsInRoleAsync(user, role))
                {
                    var addRole = await _users.AddToRoleAsync(user, role);
                    ThrowIfFailed(addRole);
                }
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id, CancellationToken ct)
        {
            if (id == CurrentUserId)
                throw new InvalidOperationException("You cannot delete your own account.");

            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            var assignments = await _db.RestaurantUserRoles
                .Where(x => x.UserId == id)
                .ToListAsync(ct);

            if (assignments.Count > 0)
                _db.RestaurantUserRoles.RemoveRange(assignments);

            var delete = await _users.DeleteAsync(user);
            ThrowIfFailed(delete);

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{id}/roles")]
        public async Task<IActionResult> GetUserRoles(string id, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            return Ok(await BuildUserDtoAsync(user, ct));
        }

        [HttpPut("{id}/roles")]
        public async Task<IActionResult> UpdateUserRoles(string id, [FromBody] UpdateUserRolesDto req, CancellationToken ct)
        {
            var user = await _users.FindByIdAsync(id);
            if (user is null)
                return NotFound();

            var requestedRoles = req.Roles?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            if (requestedRoles.Any(r => RestaurantScopedRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Restaurant roles must be assigned through restaurant associations.");

            if (id == CurrentUserId && !requestedRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("You cannot remove your own Admin access.");

            var validRoles = _roles.Roles
                .Select(r => r.Name!)
                .Where(x => !RestaurantScopedRoles.Contains(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var invalidRoles = requestedRoles.Where(r => !validRoles.Contains(r)).ToList();
            if (invalidRoles.Count > 0)
                throw new InvalidOperationException($"Invalid roles: {string.Join(", ", invalidRoles)}");

            var currentRoles = await _users.GetRolesAsync(user);
            var currentGlobalRoles = currentRoles
                .Where(x => !RestaurantScopedRoles.Contains(x, StringComparer.OrdinalIgnoreCase))
                .ToList();

            var toRemove = currentGlobalRoles.Except(requestedRoles, StringComparer.OrdinalIgnoreCase).ToList();
            var toAdd = requestedRoles.Except(currentGlobalRoles, StringComparer.OrdinalIgnoreCase).ToList();

            if (toRemove.Count > 0)
            {
                var removeResult = await _users.RemoveFromRolesAsync(user, toRemove);
                ThrowIfFailed(removeResult);
            }

            if (toAdd.Count > 0)
            {
                var addResult = await _users.AddToRolesAsync(user, toAdd);
                ThrowIfFailed(addResult);
            }

            return Ok(await BuildUserDtoAsync(user, ct));
        }

        private async Task<UserDto> BuildUserDtoAsync(ApplicationUser user, CancellationToken ct)
        {
            var roles = await _users.GetRolesAsync(user);
            var assignments = await _db.RestaurantUserRoles
                .AsNoTracking()
                .Where(x => x.UserId == user.Id)
                .Join(
                    _db.Restaurants.AsNoTracking(),
                    assignment => assignment.RestaurantId,
                    restaurant => restaurant.Id,
                    (assignment, restaurant) => new AdminUserRestaurantAssignmentDto
                    {
                        RestaurantId = assignment.RestaurantId,
                        Role = assignment.Role,
                        RestaurantName = restaurant.Name
                    })
                .OrderBy(x => x.RestaurantName)
                .ThenBy(x => x.Role)
                .ToListAsync(ct);

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                EmailConfirmed = user.EmailConfirmed,
                Roles = roles.ToList(),
                RestaurantAssignments = assignments
            };
        }

        private async Task EnsureEmailAndUsernameAvailableAsync(string email, string userName, string? currentUserId, CancellationToken ct)
        {
            var normalizedEmail = NormalizeEmail(email);
            var normalizedUserName = NormalizeUserName(userName);

            var emailOwner = await _users.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail.ToUpperInvariant(), ct);
            if (emailOwner is not null && emailOwner.Id != currentUserId)
                throw new InvalidOperationException("Email already exists.");

            var userNameOwner = await _users.Users
                .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName.ToUpperInvariant(), ct);
            if (userNameOwner is not null && userNameOwner.Id != currentUserId)
                throw new InvalidOperationException("Username already exists.");
        }

        private async Task SyncGlobalRolesAsync(ApplicationUser user, bool isAppAdmin, CancellationToken ct)
        {
            if (isAppAdmin)
            {
                if (!await _users.IsInRoleAsync(user, "Admin"))
                {
                    var addAdmin = await _users.AddToRoleAsync(user, "Admin");
                    ThrowIfFailed(addAdmin);
                }
                return;
            }

            if (await _users.IsInRoleAsync(user, "Admin"))
            {
                var removeAdmin = await _users.RemoveFromRoleAsync(user, "Admin");
                ThrowIfFailed(removeAdmin);
            }
        }

        private async Task SyncRestaurantAssignmentsAsync(
            ApplicationUser user,
            IReadOnlyCollection<AdminUserRestaurantAssignmentDto> requestedAssignments,
            CancellationToken ct)
        {
            var normalized = requestedAssignments
                .Where(x => x.RestaurantId > 0 && !string.IsNullOrWhiteSpace(x.Role))
                .Select(x => new AdminUserRestaurantAssignmentDto
                {
                    RestaurantId = x.RestaurantId,
                    Role = NormalizeRestaurantRole(x.Role)
                })
                .GroupBy(x => new { x.RestaurantId, x.Role })
                .Select(g => g.First())
                .ToList();

            if (normalized.Count > 0)
            {
                var restaurantIds = normalized.Select(x => x.RestaurantId).Distinct().ToList();
                var existingRestaurantIds = await _db.Restaurants
                    .Where(r => restaurantIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync(ct);

                var missing = restaurantIds.Except(existingRestaurantIds).ToList();
                if (missing.Count > 0)
                    throw new InvalidOperationException($"Restaurant not found: {string.Join(", ", missing)}");
            }

            var current = await _db.RestaurantUserRoles
                .Where(x => x.UserId == user.Id)
                .ToListAsync(ct);

            var toRemove = current
                .Where(existing => !normalized.Any(x => x.RestaurantId == existing.RestaurantId && x.Role == existing.Role))
                .ToList();

            foreach (var assignment in toRemove)
            {
                _db.RestaurantUserRoles.Remove(assignment);
            }

            var toAdd = normalized
                .Where(request => !current.Any(existing => existing.RestaurantId == request.RestaurantId && existing.Role == request.Role))
                .ToList();

            foreach (var assignment in toAdd)
            {
                _db.RestaurantUserRoles.Add(new RestaurantUserRole
                {
                    RestaurantId = assignment.RestaurantId,
                    UserId = user.Id,
                    Role = assignment.Role
                });
            }

            await _db.SaveChangesAsync(ct);

            foreach (var role in RestaurantScopedRoles)
            {
                var stillAssigned = await _db.RestaurantUserRoles.AnyAsync(x => x.UserId == user.Id && x.Role == role, ct);
                var isInRole = await _users.IsInRoleAsync(user, role);

                if (stillAssigned && !isInRole)
                {
                    var addRole = await _users.AddToRoleAsync(user, role);
                    ThrowIfFailed(addRole);
                }

                if (!stillAssigned && isInRole)
                {
                    var removeRole = await _users.RemoveFromRoleAsync(user, role);
                    ThrowIfFailed(removeRole);
                }
            }
        }

        private static string NormalizeEmail(string email)
        {
            var normalized = (email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Email is required.");

            return normalized;
        }

        private static string NormalizeUserName(string userName)
        {
            var normalized = (userName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Username is required.");

            return normalized;
        }

        private static string NormalizeRestaurantRole(string role)
        {
            var normalized = RestaurantScopedRoles.FirstOrDefault(x =>
                string.Equals(x, role?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalized is null)
                throw new InvalidOperationException($"Invalid restaurant role. Allowed: {string.Join(", ", RestaurantScopedRoles)}");

            return normalized;
        }

        private static void ThrowIfFailed(IdentityResult result)
        {
            if (result.Succeeded) return;

            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException(errors);
        }
    }
}
