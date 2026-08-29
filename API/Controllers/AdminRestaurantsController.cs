using Core.Contracts.Restaurants;
using Core.Contracts.Users;
using Core.Data.Entities;
using Core.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/restaurants")]
    [Authorize(Roles = "Admin,RestaurantAdmin")]
    public class AdminRestaurantsController : ControllerBase
    {
        private static readonly string[] RestaurantRoles = ["RestaurantAdmin", "Waiter", "Chef", "DeliveryDriver"];

        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly IConfiguration _cfg;

        public AdminRestaurantsController(
            AppDbContext db,
            UserManager<ApplicationUser> users,
            IEmailSender email,
            IConfiguration cfg)
        {
            _db = db;
            _users = users;
            _email = email;
            _cfg = cfg;
        }

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Missing user id.");

        private bool IsMasterAdmin => User.IsInRole("Admin");

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RestaurantDto>>> GetRestaurants(CancellationToken ct)
        {
            var query = _db.Restaurants.AsNoTracking();

            if (!IsMasterAdmin)
            {
                var userId = UserId;
                query = query.Where(r => r.UserRoles.Any(ur => ur.UserId == userId && ur.Role == "RestaurantAdmin"));
            }

            var result = await query
                .OrderBy(r => r.Name)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.City,
                    r.Street,
                    r.PostalCode,
                    r.HouseNumber,
                    r.CuisineType,
                    r.IsActive,
                    EnableTableOrders = r.Settings != null && r.Settings.EnableTableOrders,
                    EnableTakeawayOrders = r.Settings != null && r.Settings.EnableTakeawayOrders,
                    EnableDeliveryOrders = r.Settings != null && r.Settings.EnableDeliveryOrders,
                    EnableReservations = r.Settings != null && r.Settings.EnableReservations,
                    DeliveryFee = r.Settings != null ? r.Settings.DeliveryFee : 0m,
                    DeliveryRadiusKm = r.Settings != null ? r.Settings.DeliveryRadiusKm : 0m,
                    MinimumDeliveryOrder = r.Settings != null ? r.Settings.MinimumDeliveryOrder : 0m,
                    DeliveryStartMinuteOfDay = r.Settings != null ? r.Settings.DeliveryStartMinuteOfDay : 0,
                    DeliveryEndMinuteOfDay = r.Settings != null ? r.Settings.DeliveryEndMinuteOfDay : 0,
                    DeliveryLeadTimeMinutes = r.Settings != null ? r.Settings.DeliveryLeadTimeMinutes : 0
                })
                .ToListAsync(ct);

            return Ok(result.Select(r => new RestaurantDto
            {
                Id = r.Id,
                Name = r.Name,
                Address = r.Address,
                City = r.City,
                Street = r.Street,
                PostalCode = r.PostalCode,
                HouseNumber = r.HouseNumber,
                CuisineType = r.CuisineType,
                CuisineTypes = SplitCuisineTypes(r.CuisineType),
                IsActive = r.IsActive,
                EnableTableOrders = r.EnableTableOrders,
                EnableTakeawayOrders = r.EnableTakeawayOrders,
                EnableDeliveryOrders = r.EnableDeliveryOrders,
                EnableReservations = r.EnableReservations,
                DeliveryFee = r.DeliveryFee,
                DeliveryRadiusKm = r.DeliveryRadiusKm,
                MinimumDeliveryOrder = r.MinimumDeliveryOrder,
                DeliveryStartMinuteOfDay = r.DeliveryStartMinuteOfDay,
                DeliveryEndMinuteOfDay = r.DeliveryEndMinuteOfDay,
                DeliveryLeadTimeMinutes = r.DeliveryLeadTimeMinutes
            }).ToList());
        }

        [HttpGet("assignable-users")]
        public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAssignableUsers([FromQuery] string? search, CancellationToken ct)
        {
            if (!IsMasterAdmin && string.IsNullOrWhiteSpace(search))
                return Ok(Array.Empty<UserDto>());

            var normalizedSearch = search?.Trim();
            var users = await _users.Users
                .Where(u =>
                    string.IsNullOrWhiteSpace(normalizedSearch) ||
                    (u.Email != null && u.Email.Contains(normalizedSearch)) ||
                    (u.UserName != null && u.UserName.Contains(normalizedSearch)))
                .OrderBy(u => u.Email)
                .Take(20)
                .ToListAsync(ct);

            var result = new List<UserDto>();
            foreach (var user in users)
            {
                var roles = await _users.GetRolesAsync(user);
                result.Add(new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.UserName,
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                });
            }

            return Ok(result);
        }

        [HttpGet("cuisines")]
        public async Task<ActionResult<IReadOnlyList<string>>> GetCuisineOptions(CancellationToken ct)
        {
            var items = await _db.Cuisines
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => x.Name)
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<RestaurantDto>> CreateRestaurant(RestaurantCreateDto dto, CancellationToken ct)
        {
            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Restaurant name is required.");

            var restaurant = new Restaurant
            {
                Name = name,
                City = TrimOrNull(dto.City),
                Street = TrimOrNull(dto.Street),
                PostalCode = TrimOrNull(dto.PostalCode),
                HouseNumber = TrimOrNull(dto.HouseNumber),
                Address = BuildAddress(dto),
                CuisineType = NormalizeCuisineTypes(dto.CuisineTypes),
                IsActive = dto.IsActive,
                Settings = new RestaurantSettings()
            };

            _db.Restaurants.Add(restaurant);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetRestaurants), new { id = restaurant.Id }, new RestaurantDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Address = restaurant.Address,
                City = restaurant.City,
                Street = restaurant.Street,
                PostalCode = restaurant.PostalCode,
                HouseNumber = restaurant.HouseNumber,
                CuisineType = restaurant.CuisineType,
                CuisineTypes = SplitCuisineTypes(restaurant.CuisineType),
                IsActive = restaurant.IsActive
            });
        }

        [HttpPut("{restaurantId:int}")]
        public async Task<IActionResult> UpdateRestaurant(int restaurantId, RestaurantCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);
            var restaurant = await _db.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, ct);
            if (restaurant is null) return NotFound();

            var name = dto.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Restaurant name is required.");

            restaurant.Name = name;
            restaurant.City = TrimOrNull(dto.City);
            restaurant.Street = TrimOrNull(dto.Street);
            restaurant.PostalCode = TrimOrNull(dto.PostalCode);
            restaurant.HouseNumber = TrimOrNull(dto.HouseNumber);
            restaurant.Address = BuildAddress(dto);
            restaurant.CuisineType = NormalizeCuisineTypes(dto.CuisineTypes);
            restaurant.IsActive = dto.IsActive;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{restaurantId:int}/settings")]
        public async Task<ActionResult<RestaurantSettingsDto>> GetSettings(int restaurantId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);
            var settings = await EnsureSettingsAsync(restaurantId, ct);
            return Ok(MapSettings(settings));
        }

        [HttpPut("{restaurantId:int}/settings")]
        public async Task<IActionResult> UpdateSettings(int restaurantId, RestaurantSettingsUpdateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            ValidateSettings(dto);
            var settings = await EnsureSettingsAsync(restaurantId, ct);

            settings.EnableTableOrders = dto.EnableTableOrders;
            settings.EnableTakeawayOrders = dto.EnableTakeawayOrders;
            settings.EnableDeliveryOrders = dto.EnableDeliveryOrders;
            settings.EnablePayInApp = dto.EnablePayInApp;
            settings.EnablePayAtCounter = dto.EnablePayAtCounter;
            settings.EnablePayOnDelivery = dto.EnablePayOnDelivery;
            settings.EnableReservations = dto.EnableReservations;
            settings.AllowUserTableSelectionForReservations = dto.AllowUserTableSelectionForReservations;
            settings.ReservationRequiresInAppPayment = dto.ReservationRequiresInAppPayment;
            settings.ReservationPreorderMinOffsetMinutes = dto.ReservationPreorderMinOffsetMinutes;
            settings.ReservationPreorderMaxAfterStartMinutes = dto.ReservationPreorderMaxAfterStartMinutes;
            settings.ReservationStartMinuteOfDay = dto.ReservationStartMinuteOfDay;
            settings.ReservationLastStartMinuteOfDay = dto.ReservationLastStartMinuteOfDay;
            settings.DefaultReservationDurationMinutes = dto.DefaultReservationDurationMinutes;
            settings.ReservationHoldsTableUntilClose = dto.ReservationHoldsTableUntilClose;
            settings.ReservationGracePeriodMinutes = dto.ReservationGracePeriodMinutes;
            settings.DeliveryFee = dto.DeliveryFee;
            settings.DeliveryRadiusKm = dto.DeliveryRadiusKm;
            settings.MinimumDeliveryOrder = dto.MinimumDeliveryOrder;
            settings.DeliveryStartMinuteOfDay = dto.DeliveryStartMinuteOfDay;
            settings.DeliveryEndMinuteOfDay = dto.DeliveryEndMinuteOfDay;
            settings.DeliveryLeadTimeMinutes = dto.DeliveryLeadTimeMinutes;
            settings.DeliveryAssignmentMode = Enum.IsDefined(typeof(DeliveryAssignmentMode), dto.DeliveryAssignmentMode)
                ? (DeliveryAssignmentMode)dto.DeliveryAssignmentMode
                : throw new InvalidOperationException("Invalid delivery assignment mode.");
            settings.EstimatedPreparationBaseMinutes = dto.EstimatedPreparationBaseMinutes;
            settings.EstimatedPreparationPerItemMinutes = dto.EstimatedPreparationPerItemMinutes;
            settings.ExtraIngredientPrice = dto.ExtraIngredientPrice;
            settings.SupportedCultures = NormalizeCultures(dto.SupportedCultures);
            settings.DefaultCulture = NormalizeCulture(dto.DefaultCulture);

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{restaurantId:int}/tables")]
        public async Task<ActionResult<IReadOnlyList<RestaurantTableDto>>> GetTables(int restaurantId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var result = await _db.RestaurantTables
                .AsNoTracking()
                .Where(t => t.RestaurantId == restaurantId)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Label)
                .Select(t => new RestaurantTableDto
                {
                    Id = t.Id,
                    RestaurantId = t.RestaurantId,
                    Label = t.Label,
                    Seats = t.Seats,
                    IsActive = t.IsActive,
                    IsReservable = t.IsReservable,
                    SortOrder = t.SortOrder
                })
                .ToListAsync(ct);

            return Ok(result);
        }

        [HttpPost("{restaurantId:int}/tables")]
        public async Task<IActionResult> CreateTable(int restaurantId, RestaurantTableCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var label = dto.Label?.Trim();
            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("Table label is required.");

            _db.RestaurantTables.Add(new RestaurantTable
            {
                RestaurantId = restaurantId,
                Label = label,
                Seats = dto.Seats,
                IsActive = dto.IsActive,
                IsReservable = dto.IsReservable,
                SortOrder = dto.SortOrder
            });

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpPut("{restaurantId:int}/tables/{tableId:int}")]
        public async Task<IActionResult> UpdateTable(int restaurantId, int tableId, RestaurantTableCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId, ct);

            if (table is null) return NotFound();

            var label = dto.Label?.Trim();
            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("Table label is required.");

            table.Label = label;
            table.Seats = dto.Seats;
            table.IsActive = dto.IsActive;
            table.IsReservable = dto.IsReservable;
            table.SortOrder = dto.SortOrder;

            if (!table.IsActive || !table.IsReservable)
            {
                var schedules = await _db.ReservationSchedules
                    .Where(x => x.RestaurantTableId == table.Id)
                    .ToListAsync(ct);
                if (schedules.Count > 0)
                {
                    _db.ReservationSchedules.RemoveRange(schedules);
                }
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{restaurantId:int}/tables/{tableId:int}")]
        public async Task<IActionResult> DeleteTable(int restaurantId, int tableId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId, ct);

            if (table is null) return NotFound();

            var schedules = await _db.ReservationSchedules
                .Where(x => x.RestaurantTableId == table.Id)
                .ToListAsync(ct);
            if (schedules.Count > 0)
            {
                _db.ReservationSchedules.RemoveRange(schedules);
            }

            _db.RestaurantTables.Remove(table);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{restaurantId:int}/users")]
        public async Task<ActionResult<IReadOnlyList<RestaurantUserRoleDto>>> GetUserRoles(int restaurantId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var assignments = await _db.RestaurantUserRoles
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId)
                .OrderBy(x => x.Role)
                .ThenBy(x => x.UserId)
                .ToListAsync(ct);

            var invites = await _db.RestaurantStaffInvites
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId && x.ExpiresAt > DateTime.UtcNow)
                .OrderBy(x => x.Email)
                .ToListAsync(ct);

            var result = new List<RestaurantUserRoleDto>();
            foreach (var assignment in assignments)
            {
                var user = await _users.FindByIdAsync(assignment.UserId);
                result.Add(new RestaurantUserRoleDto
                {
                    Id = assignment.Id,
                    RestaurantId = assignment.RestaurantId,
                    UserId = assignment.UserId,
                    Email = user?.Email,
                    Role = assignment.Role
                });
            }

            foreach (var invite in invites)
            {
                var alreadyAssigned = result.Any(x =>
                    string.Equals(x.Email, invite.Email, StringComparison.OrdinalIgnoreCase));
                if (alreadyAssigned)
                    continue;

                result.Add(new RestaurantUserRoleDto
                {
                    Id = invite.Id,
                    InviteId = invite.Id,
                    RestaurantId = invite.RestaurantId,
                    UserId = invite.UserId ?? $"invite:{invite.Id}",
                    Email = invite.Email,
                    Role = invite.UserId is null ? "Pending" : "Awaiting Assignment",
                    IsPendingInvite = invite.UserId is null,
                    IsAwaitingAssignment = invite.UserId is not null
                });
            }

            return Ok(result);
        }

        [HttpPost("{restaurantId:int}/users")]
        public async Task<IActionResult> AssignUserRole(int restaurantId, RestaurantUserRoleUpsertDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var role = NormalizeRestaurantRole(dto.Role);
            if (!IsMasterAdmin && role == "RestaurantAdmin")
                throw new InvalidOperationException("Only master admins can appoint restaurant admins.");

            var user = await _users.FindByIdAsync(dto.UserId);
            if (user is null) return NotFound();

            var exists = await _db.RestaurantUserRoles.AnyAsync(
                x => x.RestaurantId == restaurantId && x.UserId == dto.UserId && x.Role == role,
                ct);

            if (!exists)
            {
                _db.RestaurantUserRoles.Add(new RestaurantUserRole
                {
                    RestaurantId = restaurantId,
                    UserId = dto.UserId,
                    Role = role
                });
            }

            var linkedInvites = await _db.RestaurantStaffInvites
                .Where(x => x.RestaurantId == restaurantId && x.UserId == dto.UserId)
                .ToListAsync(ct);
            if (linkedInvites.Count > 0)
            {
                _db.RestaurantStaffInvites.RemoveRange(linkedInvites);
            }

            if (!await _users.IsInRoleAsync(user, role))
                await _users.AddToRoleAsync(user, role);

            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                var restaurantName = await _db.Restaurants
                    .AsNoTracking()
                    .Where(x => x.Id == restaurantId)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync(ct);
                await _email.SendAsync(
                    user.Email,
                    $"Food Order App - Role assigned for {restaurantName ?? "restaurant"}",
                    $@"<p>Your role for <strong>{restaurantName ?? "the restaurant"}</strong> has been assigned as <strong>{role}</strong>.</p>",
                    ct: ct);
            }
            return NoContent();
        }

        [HttpPost("{restaurantId:int}/invites")]
        public async Task<IActionResult> InviteUser(int restaurantId, RestaurantInviteCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var email = (dto.Email ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                throw new InvalidOperationException("A valid email address is required.");

            var restaurant = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(x => x.Id == restaurantId, ct);
            if (restaurant is null)
                return NotFound();

            var invite = await _db.RestaurantStaffInvites
                .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId
                    && x.Email == email
                    && x.ExpiresAt > DateTime.UtcNow, ct);

            if (invite is null)
            {
                invite = new RestaurantStaffInvite
                {
                    RestaurantId = restaurantId,
                    Email = email,
                    RequestedRole = "AwaitingAssignment",
                    InviteToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)),
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    InvitedByUserId = UserId
                };
                _db.RestaurantStaffInvites.Add(invite);
            }

            var existingUser = await _users.FindByEmailAsync(email);
            if (existingUser is not null && existingUser.EmailConfirmed)
            {
                invite.UserId = existingUser.Id;
                invite.AcceptedAt ??= DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);

            var frontendBaseUrl = _cfg["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5173";
            var inviteLink = $"{frontendBaseUrl}/register?restaurantInvite={Uri.EscapeDataString(invite.InviteToken)}";
            await _email.SendAsync(
                email,
                $"Food Order App - Join {restaurant.Name}",
                $@"
                <p>You have been invited to join <strong>{restaurant.Name}</strong>.</p>
                <p>Create or activate your account with this email address, then open this link:</p>
                <p><a href=""{inviteLink}"">{inviteLink}</a></p>
                <p>Once your account is active, you will appear in the restaurant staff list for final assignment.</p>",
                ct: ct);

            return NoContent();
        }

        [HttpDelete("{restaurantId:int}/users/{assignmentId:int}")]
        public async Task<IActionResult> RemoveUserRole(int restaurantId, int assignmentId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var assignment = await _db.RestaurantUserRoles
                .FirstOrDefaultAsync(x => x.Id == assignmentId && x.RestaurantId == restaurantId, ct);

            if (assignment is null) return NotFound();

            if (!IsMasterAdmin && assignment.Role == "RestaurantAdmin")
                throw new InvalidOperationException("Only master admins can remove restaurant admins.");

            _db.RestaurantUserRoles.Remove(assignment);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{restaurantId:int}/invites/{inviteId:int}")]
        public async Task<IActionResult> RemoveInvite(int restaurantId, int inviteId, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(restaurantId, ct);

            var invite = await _db.RestaurantStaffInvites
                .FirstOrDefaultAsync(x => x.Id == inviteId && x.RestaurantId == restaurantId, ct);

            if (invite is null) return NotFound();

            _db.RestaurantStaffInvites.Remove(invite);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task EnsureCanManageRestaurantAsync(int restaurantId, CancellationToken ct)
        {
            var restaurantExists = await _db.Restaurants.AnyAsync(r => r.Id == restaurantId, ct);
            if (!restaurantExists)
                throw new KeyNotFoundException("Restaurant not found.");

            if (IsMasterAdmin) return;

            var userId = UserId;
            var canManage = await _db.RestaurantUserRoles.AnyAsync(
                x => x.RestaurantId == restaurantId && x.UserId == userId && x.Role == "RestaurantAdmin",
                ct);

            if (!canManage)
                throw new InvalidOperationException("You are not allowed to manage this restaurant.");
        }

        private static string NormalizeRestaurantRole(string role)
        {
            var normalized = RestaurantRoles.FirstOrDefault(x =>
                string.Equals(x, role?.Trim(), StringComparison.OrdinalIgnoreCase));

            if (normalized is null)
                throw new InvalidOperationException($"Invalid restaurant role. Allowed: {string.Join(", ", RestaurantRoles)}");

            return normalized;
        }

        private async Task<RestaurantSettings> EnsureSettingsAsync(int restaurantId, CancellationToken ct)
        {
            var settings = await _db.RestaurantSettings
                .FirstOrDefaultAsync(x => x.RestaurantId == restaurantId, ct);

            if (settings is not null)
                return settings;

            settings = new RestaurantSettings { RestaurantId = restaurantId };
            _db.RestaurantSettings.Add(settings);
            await _db.SaveChangesAsync(ct);
            return settings;
        }

        private static RestaurantSettingsDto MapSettings(RestaurantSettings settings) => new()
        {
            RestaurantId = settings.RestaurantId,
            EnableTableOrders = settings.EnableTableOrders,
            EnableTakeawayOrders = settings.EnableTakeawayOrders,
            EnableDeliveryOrders = settings.EnableDeliveryOrders,
            EnablePayInApp = settings.EnablePayInApp,
            EnablePayAtCounter = settings.EnablePayAtCounter,
            EnablePayOnDelivery = settings.EnablePayOnDelivery,
            EnableReservations = settings.EnableReservations,
            AllowUserTableSelectionForReservations = settings.AllowUserTableSelectionForReservations,
            ReservationRequiresInAppPayment = settings.ReservationRequiresInAppPayment,
            ReservationPreorderMinOffsetMinutes = settings.ReservationPreorderMinOffsetMinutes,
            ReservationPreorderMaxAfterStartMinutes = settings.ReservationPreorderMaxAfterStartMinutes,
            ReservationStartMinuteOfDay = settings.ReservationStartMinuteOfDay,
            ReservationLastStartMinuteOfDay = settings.ReservationLastStartMinuteOfDay,
            DefaultReservationDurationMinutes = settings.DefaultReservationDurationMinutes,
            ReservationHoldsTableUntilClose = settings.ReservationHoldsTableUntilClose,
            ReservationGracePeriodMinutes = settings.ReservationGracePeriodMinutes,
            DeliveryFee = settings.DeliveryFee,
            DeliveryRadiusKm = settings.DeliveryRadiusKm,
            MinimumDeliveryOrder = settings.MinimumDeliveryOrder,
            DeliveryStartMinuteOfDay = settings.DeliveryStartMinuteOfDay,
            DeliveryEndMinuteOfDay = settings.DeliveryEndMinuteOfDay,
            DeliveryLeadTimeMinutes = settings.DeliveryLeadTimeMinutes,
            DeliveryAssignmentMode = (int)settings.DeliveryAssignmentMode,
            EstimatedPreparationBaseMinutes = settings.EstimatedPreparationBaseMinutes,
            EstimatedPreparationPerItemMinutes = settings.EstimatedPreparationPerItemMinutes,
            ExtraIngredientPrice = settings.ExtraIngredientPrice,
            SupportedCultures = settings.SupportedCultures,
            DefaultCulture = settings.DefaultCulture
        };

        private static void ValidateSettings(RestaurantSettingsUpdateDto dto)
        {
            if (!dto.EnableTableOrders && !dto.EnableTakeawayOrders && !dto.EnableDeliveryOrders)
                throw new InvalidOperationException("At least one order type must be enabled.");

            if (!dto.EnablePayInApp && !dto.EnablePayAtCounter)
                throw new InvalidOperationException("At least one payment method must be enabled.");

            if (dto.DeliveryFee < 0)
                throw new InvalidOperationException("Delivery fee cannot be negative.");

            if (dto.DeliveryRadiusKm < 0)
                throw new InvalidOperationException("Delivery radius cannot be negative.");

            if (dto.MinimumDeliveryOrder < 0)
                throw new InvalidOperationException("Minimum delivery order cannot be negative.");

            if (dto.DeliveryStartMinuteOfDay < 0 || dto.DeliveryStartMinuteOfDay > 24 * 60)
                throw new InvalidOperationException("Delivery start minute is invalid.");

            if (dto.DeliveryEndMinuteOfDay < 0 || dto.DeliveryEndMinuteOfDay > 24 * 60)
                throw new InvalidOperationException("Delivery end minute is invalid.");

            if (dto.DeliveryLeadTimeMinutes < 0)
                throw new InvalidOperationException("Delivery lead time cannot be negative.");

            if (dto.EstimatedPreparationBaseMinutes < 0 || dto.EstimatedPreparationPerItemMinutes < 0)
                throw new InvalidOperationException("Preparation estimates cannot be negative.");

            if (dto.ExtraIngredientPrice < 0)
                throw new InvalidOperationException("Extra ingredient price cannot be negative.");

            if (dto.ReservationPreorderMinOffsetMinutes < 0 || dto.ReservationPreorderMaxAfterStartMinutes < 0)
                throw new InvalidOperationException("Reservation windows cannot be negative.");

            if (dto.ReservationPreorderMaxAfterStartMinutes < dto.ReservationPreorderMinOffsetMinutes)
                throw new InvalidOperationException("Reservation max window cannot be lower than min offset.");

            if (dto.ReservationStartMinuteOfDay < 0 || dto.ReservationStartMinuteOfDay > 24 * 60)
                throw new InvalidOperationException("Reservation start minute is invalid.");

            if (dto.ReservationLastStartMinuteOfDay < 0 || dto.ReservationLastStartMinuteOfDay > 24 * 60)
                throw new InvalidOperationException("Reservation last reservation minute is invalid.");

            if (dto.DefaultReservationDurationMinutes <= 0)
                throw new InvalidOperationException("Default reservation duration must be greater than zero.");

            if (dto.ReservationGracePeriodMinutes < 0)
                throw new InvalidOperationException("Reservation grace period cannot be negative.");

            var cultures = NormalizeCultures(dto.SupportedCultures)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var defaultCulture = NormalizeCulture(dto.DefaultCulture);

            if (!cultures.Contains(defaultCulture, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Default culture must be included in supported cultures.");
        }

        private static string NormalizeCultures(string? cultures)
        {
            var normalized = (cultures ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeCulture)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (normalized.Count == 0)
                throw new InvalidOperationException("At least one culture is required.");

            return string.Join(",", normalized);
        }

        private static string NormalizeCulture(string? culture)
        {
            var normalized = culture?.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
                throw new InvalidOperationException("Culture is required.");

            if (normalized.Length > 20)
                throw new InvalidOperationException("Culture is too long.");

            return normalized;
        }

        private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string? BuildAddress(RestaurantCreateDto dto)
        {
            var parts = new[]
            {
                string.Join(" ", new[] { TrimOrNull(dto.Street), TrimOrNull(dto.HouseNumber) }.Where(x => !string.IsNullOrWhiteSpace(x))),
                TrimOrNull(dto.PostalCode),
                TrimOrNull(dto.City)
            }.Where(x => !string.IsNullOrWhiteSpace(x));

            var combined = string.Join(", ", parts);
            return string.IsNullOrWhiteSpace(combined) ? null : combined;
        }

        private static string? NormalizeCuisineTypes(IReadOnlyCollection<string>? values)
        {
            var normalized = (values ?? Array.Empty<string>())
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            return normalized.Count == 0 ? null : string.Join(",", normalized);
        }

        private static List<string> SplitCuisineTypes(string? value)
            => string.IsNullOrWhiteSpace(value)
                ? new List<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
