using Core.Contracts.Reservations;
using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using Infrastructure.Auth;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/reservations")]
    [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
    public class AdminReservationsController : ControllerBase
    {
        private const int SlotMinutes = 15;
        private static readonly ReservationStatus[] BlockingStatuses =
        [
            ReservationStatus.Requested,
            ReservationStatus.Confirmed,
            ReservationStatus.Seated
        ];

        private static readonly ReservationStatus[] AutoReleaseStatuses =
        [
            ReservationStatus.Requested,
            ReservationStatus.Confirmed
        ];

        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _email;
        private readonly INotificationService _notifications;

        public AdminReservationsController(
            AppDbContext db,
            UserManager<ApplicationUser> users,
            IEmailSender email,
            INotificationService notifications)
        {
            _db = db;
            _users = users;
            _email = email;
            _notifications = notifications;
        }

        [HttpGet("context")]
        public async Task<ActionResult<AdminReservationRestaurantDto>> GetContext(CancellationToken ct)
        {
            var restaurants = await GetAccessibleRestaurantsAsync(ct);
            var first = restaurants.FirstOrDefault();
            if (first is null)
                return NotFound();

            return Ok(first);
        }

        [HttpGet("restaurants")]
        public async Task<ActionResult<IReadOnlyList<AdminReservationRestaurantDto>>> GetRestaurants(CancellationToken ct)
        {
            return Ok(await GetAccessibleRestaurantsAsync(ct));
        }

        [HttpGet("availability")]
        public async Task<ActionResult<ReservationAvailabilityDto>> GetAvailability(
            [FromQuery] int restaurantId,
            [FromQuery] DateTime date,
            CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(restaurantId, ct);
            return Ok(await BuildAvailabilityAsync(restaurantId, date, ct));
        }

        [HttpGet("schedules")]
        public async Task<ActionResult<IReadOnlyList<ReservationScheduleDto>>> GetSchedules([FromQuery] int restaurantId, CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(restaurantId, ct);

            var result = await _db.ReservationSchedules
                .AsNoTracking()
                .Include(x => x.RestaurantTable)
                .Where(x => x.RestaurantId == restaurantId)
                .Where(x => x.RestaurantTable != null && x.RestaurantTable.IsActive && x.RestaurantTable.IsReservable)
                .OrderBy(x => x.StartMinuteOfDay)
                .ThenBy(x => x.RestaurantTable!.SortOrder)
                .ThenBy(x => x.RestaurantTable!.Label)
                .Select(x => new ReservationScheduleDto
                {
                    Id = x.Id,
                    RestaurantId = x.RestaurantId,
                    RestaurantTableId = x.RestaurantTableId,
                    TableLabel = x.RestaurantTable == null ? null : x.RestaurantTable.Label,
                    Seats = x.RestaurantTable == null ? null : x.RestaurantTable.Seats,
                    StartMinuteOfDay = x.StartMinuteOfDay,
                    EndMinuteOfDay = x.EndMinuteOfDay,
                    IntervalMinutes = x.IntervalMinutes,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(ct);

            return Ok(result);
        }

        [HttpPost("schedules")]
        public async Task<IActionResult> CreateSchedule(ReservationScheduleCreateDto dto, CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(dto.RestaurantId, ct);

            if (dto.TableIds.Count == 0)
                throw new InvalidOperationException("Choose at least one table.");

            if (dto.IntervalMinutes < SlotMinutes || dto.IntervalMinutes % SlotMinutes != 0)
                throw new InvalidOperationException("Interval must be a multiple of 15 minutes.");

            var restaurant = await GetRestaurantWithSettingsAsync(dto.RestaurantId, ct);
            if (restaurant is null)
                return NotFound();

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            var startMinuteOfDay = ParseMinuteOfDay(dto.StartTime, "start time");
            var endMinuteOfDay = ParseMinuteOfDay(dto.EndTime, "end time");

            var scheduleSegments = BuildScheduleSegments(startMinuteOfDay, endMinuteOfDay, dto.IntervalMinutes);
            if (scheduleSegments.Count == 0)
                throw new InvalidOperationException("Schedule must contain at least one reservation slot.");

            if (scheduleSegments.Any(segment =>
                    !IsMinuteAllowed(settings, segment.StartMinuteOfDay) ||
                    !IsMinuteAllowed(settings, segment.EndMinuteOfDay)))
            {
                throw new InvalidOperationException("Schedule must fit inside restaurant reservation hours.");
            }

            var tables = await _db.RestaurantTables
                .Where(x => x.RestaurantId == dto.RestaurantId && dto.TableIds.Contains(x.Id) && x.IsActive && x.IsReservable)
                .ToListAsync(ct);

            if (tables.Count != dto.TableIds.Distinct().Count())
                throw new InvalidOperationException("One or more selected tables are not reservable.");

            var tableIds = dto.TableIds.Distinct().ToList();

            var existing = await _db.ReservationSchedules
                .Where(x => x.RestaurantId == dto.RestaurantId && tableIds.Contains(x.RestaurantTableId))
                .ToListAsync(ct);

            if (existing.Count > 0)
            {
                _db.ReservationSchedules.RemoveRange(existing);
            }

            foreach (var tableId in tableIds)
            {
                foreach (var segment in scheduleSegments)
                {
                    _db.ReservationSchedules.Add(new ReservationSchedule
                    {
                        RestaurantId = dto.RestaurantId,
                        RestaurantTableId = tableId,
                        StartMinuteOfDay = segment.StartMinuteOfDay,
                        EndMinuteOfDay = segment.EndMinuteOfDay,
                        IntervalMinutes = dto.IntervalMinutes,
                        IsActive = true
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("schedules/{id:int}")]
        public async Task<IActionResult> DeleteSchedule(int id, CancellationToken ct)
        {
            var schedule = await _db.ReservationSchedules.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (schedule is null) return NotFound();

            await EnsureCanAccessRestaurantAsync(schedule.RestaurantId, ct);
            _db.ReservationSchedules.Remove(schedule);
            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("schedules")]
        public async Task<IActionResult> ClearSchedules([FromQuery] int restaurantId, CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(restaurantId, ct);

            var schedules = await _db.ReservationSchedules
                .Where(x => x.RestaurantId == restaurantId)
                .ToListAsync(ct);

            if (schedules.Count > 0)
            {
                _db.ReservationSchedules.RemoveRange(schedules);
                await _db.SaveChangesAsync(ct);
            }

            return NoContent();
        }

        [HttpGet("calendar")]
        public async Task<ActionResult<IReadOnlyList<ReservationCalendarDayDto>>> GetCalendar(
            [FromQuery] int restaurantId,
            [FromQuery] int year,
            [FromQuery] int month,
            CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(restaurantId, ct);

            var firstLocal = new DateTime(year, month, 1);
            var nextLocal = firstLocal.AddMonths(1);
            var firstUtc = ToUtc(firstLocal);
            var nextUtc = ToUtc(nextLocal);

            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId)
                .Where(x => x.StartAt >= firstUtc && x.StartAt < nextUtc)
                .ToListAsync(ct);

            var result = reservations
                .GroupBy(x => ToLocalTime(x.StartAt).Date)
                .Select(g => new ReservationCalendarDayDto
                {
                    Date = g.Key,
                    ReservationCount = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReservationDto>> GetById(int id, CancellationToken ct)
        {
            var reservation = await _db.Reservations
                .AsNoTracking()
                .Include(x => x.Restaurant)
                .Include(x => x.RestaurantTable)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (reservation is null)
                return NotFound();

            await EnsureCanAccessRestaurantAsync(reservation.RestaurantId, ct);
            return Ok(MapReservation(reservation));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<ReservationDto>>> Get(
            [FromQuery] int? restaurantId,
            [FromQuery] DateTime? date,
            [FromQuery] string? search,
            CancellationToken ct,
            [FromQuery] bool historyOnly = false,
            [FromQuery] int? take = null)
        {
            if (restaurantId.HasValue)
                await EnsureCanAccessRestaurantAsync(restaurantId.Value, ct);

            await AutoReleaseExpiredReservationsAsync(restaurantId, ct);
            var allowedIds = await GetAllowedRestaurantIdsAsync(ct);

            var query = _db.Reservations
                .AsNoTracking()
                .Include(x => x.Restaurant)
                .Include(x => x.RestaurantTable)
                .Where(x => !restaurantId.HasValue || x.RestaurantId == restaurantId.Value)
                .Where(x => allowedIds == null || allowedIds.Contains(x.RestaurantId));

            if (date.HasValue)
            {
                var localDate = NormalizeDate(date.Value);
                var dayWindow = GetDayWindowUtc(localDate);
                query = query.Where(x => x.StartAt >= dayWindow.StartUtc && x.StartAt < dayWindow.EndUtc);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalized = search.Trim();
                query = query.Where(x =>
                    (x.GuestName != null && x.GuestName.Contains(normalized)) ||
                    (x.GuestEmail != null && x.GuestEmail.Contains(normalized)) ||
                    (x.GuestPhone != null && x.GuestPhone.Contains(normalized)) ||
                    (x.Note != null && x.Note.Contains(normalized)) ||
                    (x.RestaurantTable != null && x.RestaurantTable.Label.Contains(normalized)) ||
                    (x.Restaurant != null && x.Restaurant.Name.Contains(normalized)));
            }

            if (historyOnly)
            {
                query = query.Where(x => x.StartAt < DateTime.UtcNow);
            }

            IQueryable<Reservation> resultQuery = query.OrderByDescending(x => x.StartAt);
            var effectiveTake = take.HasValue
                ? Math.Clamp(take.Value, 1, 500)
                : historyOnly || !string.IsNullOrWhiteSpace(search)
                    ? 200
                    : (int?)null;

            if (effectiveTake.HasValue)
            {
                resultQuery = resultQuery.Take(effectiveTake.Value);
            }

            var result = await resultQuery
                .ToListAsync(ct);

            return Ok(result.Select(x => MapReservation(x)).ToList());
        }

        [HttpPost]
        public async Task<ActionResult<ReservationDto>> Create(AdminReservationCreateDto dto, CancellationToken ct)
        {
            await EnsureCanAccessRestaurantAsync(dto.RestaurantId, ct);

            var restaurant = await GetRestaurantWithSettingsAsync(dto.RestaurantId, ct);
            if (restaurant is null)
                return NotFound();

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            if (!settings.EnableReservations)
                throw new InvalidOperationException("Reservations are not enabled for this restaurant.");

            var guestName = TrimOrNull(dto.GuestName);
            if (string.IsNullOrWhiteSpace(guestName))
                throw new InvalidOperationException("Guest name is required.");

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.RestaurantTableId &&
                    x.RestaurantId == dto.RestaurantId &&
                    x.IsActive &&
                    x.IsReservable,
                    ct);

            if (table is null)
                throw new InvalidOperationException("Selected table is not available for reservations.");

            await AutoReleaseExpiredReservationsAsync(dto.RestaurantId, ct);

            var requestedStartUtc = NormalizeIncomingUtc(dto.StartAt);
            var requestedStartLocal = ToLocalTime(requestedStartUtc);
            var endUtc = ComputeEndUtc(requestedStartUtc, settings);

            await EnsureReservationAllowedByScheduleAsync(dto.RestaurantId, table.Id, requestedStartUtc, requestedStartLocal, settings, ct);
            await EnsureNoOverlapAsync(table.Id, requestedStartUtc, endUtc, ct);

            var reservation = new Reservation
            {
                RestaurantId = dto.RestaurantId,
                RestaurantTableId = table.Id,
                CustomerId = null,
                GuestName = guestName,
                GuestEmail = TrimOrNull(dto.GuestEmail),
                GuestPhone = TrimOrNull(dto.GuestPhone),
                PartySize = Math.Max(1, dto.PartySize),
                StartAt = requestedStartUtc,
                EndAt = endUtc,
                Note = TrimOrNull(dto.Note),
                Status = ReservationStatus.Requested
            };

            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { id = reservation.Id }, MapReservation(reservation, restaurant.Name, table.Label));
        }

        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromQuery] ReservationStatus status, CancellationToken ct)
        {
            var reservation = await _db.Reservations.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (reservation is null) return NotFound();

            await EnsureCanAccessRestaurantAsync(reservation.RestaurantId, ct);
            reservation.Status = status;

            if (status == ReservationStatus.Cancelled)
                reservation.CancelledAt ??= DateTime.UtcNow;

            if (status == ReservationStatus.NoShow)
            {
                reservation.ReleasedAt ??= DateTime.UtcNow;
                await NotifyNoShowAsync(reservation, ct);
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        private async Task NotifyNoShowAsync(Reservation reservation, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reservation.CustomerId))
                return;

            var user = await _users.FindByIdAsync(reservation.CustomerId);
            if (user is not null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _email.SendAsync(
                    user.Email,
                    "Food Order App - Reservation marked as no-show",
                    BuildNoShowHtml(reservation),
                    ct: ct);
            }

            await _notifications.CreateAsync(
                reservation.CustomerId,
                NotificationType.ReservationNoShow,
                "Reservation marked as no-show",
                $"Your reservation #{reservation.Id} was marked as no-show.",
                JsonSerializer.Serialize(new
                {
                    type = "reservation-no-show",
                    reservationId = reservation.Id,
                    url = "/reservations"
                }),
                ct);
        }

        private async Task<ReservationAvailabilityDto> BuildAvailabilityAsync(int restaurantId, DateTime date, CancellationToken ct)
        {
            var restaurant = await GetRestaurantWithSettingsAsync(restaurantId, ct)
                ?? throw new KeyNotFoundException("Restaurant not found.");

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            if (!settings.EnableReservations)
                throw new InvalidOperationException("Reservations are not enabled for this restaurant.");

            var localDate = NormalizeDate(date);
            await AutoReleaseExpiredReservationsAsync(restaurantId, ct);

            var tables = await _db.RestaurantTables
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId && x.IsActive && x.IsReservable)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Label)
                .ToListAsync(ct);

            var schedules = await _db.ReservationSchedules
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId && x.IsActive)
                .ToListAsync(ct);

            var dayWindow = GetDayWindowUtc(localDate);
            var reservations = await _db.Reservations
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId)
                .Where(x => x.RestaurantTableId != null)
                .Where(x => BlockingStatuses.Contains(x.Status))
                .Where(x => x.StartAt < dayWindow.EndUtc && x.EndAt > dayWindow.StartUtc)
                .ToListAsync(ct);

            return new ReservationAvailabilityDto
            {
                RestaurantId = restaurantId,
                Date = localDate,
                SlotMinutes = SlotMinutes,
                Tables = tables.Select(table =>
                {
                    var tableReservations = reservations
                        .Where(x => x.RestaurantTableId == table.Id)
                        .OrderBy(x => x.StartAt)
                        .ToList();

                    var tableSchedules = schedules
                        .Where(x => x.RestaurantTableId == table.Id)
                        .OrderBy(x => x.StartMinuteOfDay)
                        .ToList();

                    var slots = BuildAvailableSlotsFromSchedules(localDate, settings, tableSchedules, tableReservations);
                    var blockedRanges = tableReservations.Select(x => new ReservationBlockedRangeDto
                    {
                        StartTime = ToLocalTimeString(x.StartAt),
                        EndTime = ToLocalTimeString(x.EndAt),
                        Label = $"Reserved from {ToLocalTimeString(x.StartAt)}"
                    }).ToList();

                    return new ReservationTableAvailabilityDto
                    {
                        TableId = table.Id,
                        Label = table.Label,
                        Seats = table.Seats,
                        AvailableStartTimes = slots,
                        BlockedRanges = blockedRanges
                    };
                }).ToList()
            };
        }

        private async Task<Restaurant?> GetRestaurantWithSettingsAsync(int restaurantId, CancellationToken ct)
        {
            return await _db.Restaurants
                .Include(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == restaurantId && x.IsActive, ct);
        }

        private async Task EnsureReservationAllowedByScheduleAsync(
            int restaurantId,
            int tableId,
            DateTime requestedStartUtc,
            DateTime requestedStartLocal,
            RestaurantSettings settings,
            CancellationToken ct)
        {
            ValidateRequestedStart(settings, requestedStartLocal);
            var requestedMinuteOfDay = (requestedStartLocal.Hour * 60) + requestedStartLocal.Minute;

            var schedules = await _db.ReservationSchedules
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId && x.RestaurantTableId == tableId && x.IsActive)
                .ToListAsync(ct);

            var allowed = schedules.Any(x =>
                requestedMinuteOfDay >= x.StartMinuteOfDay &&
                requestedMinuteOfDay <= x.EndMinuteOfDay &&
                (requestedMinuteOfDay - x.StartMinuteOfDay) % x.IntervalMinutes == 0);

            if (!allowed)
                throw new InvalidOperationException("Selected time is not available in the reservation schedule.");
        }

        private async Task EnsureNoOverlapAsync(int tableId, DateTime requestedStartUtc, DateTime endUtc, CancellationToken ct)
        {
            var overlaps = await _db.Reservations.AnyAsync(x =>
                x.RestaurantTableId == tableId &&
                BlockingStatuses.Contains(x.Status) &&
                requestedStartUtc < x.EndAt &&
                endUtc > x.StartAt,
                ct);

            if (overlaps)
                throw new InvalidOperationException("Selected table is already reserved for that time.");
        }

        private async Task AutoReleaseExpiredReservationsAsync(int? restaurantId, CancellationToken ct)
        {
            var query = _db.Reservations
                .Include(x => x.Restaurant)
                .ThenInclude(x => x!.Settings)
                .Where(x => AutoReleaseStatuses.Contains(x.Status));

            if (restaurantId.HasValue)
                query = query.Where(x => x.RestaurantId == restaurantId.Value);

            var reservations = await query.ToListAsync(ct);
            var nowUtc = DateTime.UtcNow;
            var changed = false;

            foreach (var reservation in reservations)
            {
                var grace = reservation.Restaurant?.Settings?.ReservationGracePeriodMinutes ?? 15;
                if (reservation.StartAt.AddMinutes(Math.Max(grace, 0)) < nowUtc)
                {
                    reservation.Status = ReservationStatus.NoShow;
                    reservation.ReleasedAt ??= nowUtc;
                    changed = true;
                }
            }

            if (changed)
                await _db.SaveChangesAsync(ct);
        }

        private async Task<List<int>?> GetAllowedRestaurantIdsAsync(CancellationToken ct)
        {
            if (User.IsInRole("Admin")) return null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");

            return await _db.RestaurantUserRoles
                .Where(x => x.UserId == userId && (x.Role == "RestaurantAdmin" || x.Role == "Waiter"))
                .Select(x => x.RestaurantId)
                .Distinct()
                .ToListAsync(ct);
        }

        private async Task<List<AdminReservationRestaurantDto>> GetAccessibleRestaurantsAsync(CancellationToken ct)
        {
            var allowedIds = await GetAllowedRestaurantIdsAsync(ct);
            IQueryable<Restaurant> query = _db.Restaurants.AsNoTracking().Where(x => x.IsActive);
            if (allowedIds != null)
                query = query.Where(x => allowedIds.Contains(x.Id));

            return await query
                .OrderBy(x => x.Name)
                .Select(x => new AdminReservationRestaurantDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);
        }

        private async Task EnsureCanAccessRestaurantAsync(int restaurantId, CancellationToken ct)
        {
            if (User.IsInRole("Admin")) return;

            var ids = await GetAllowedRestaurantIdsAsync(ct);
            if (ids == null || !ids.Contains(restaurantId))
                throw new InvalidOperationException("You are not allowed to access this reservation.");
        }

        private static List<string> BuildAvailableSlotsFromSchedules(
            DateTime localDate,
            RestaurantSettings settings,
            IReadOnlyList<ReservationSchedule> schedules,
            IReadOnlyList<Reservation> reservations)
        {
            var slots = new List<string>();
            var nowLocal = DateTime.Now;
            var earliestAllowedLocal = localDate.Date == nowLocal.Date
                ? RoundUpToNextSlot(nowLocal)
                : localDate.Date;

            foreach (var schedule in schedules)
            {
                for (var minute = schedule.StartMinuteOfDay; minute <= schedule.EndMinuteOfDay; minute += schedule.IntervalMinutes)
                {
                    var slotLocal = localDate.Date.AddMinutes(minute);
                    if (slotLocal < earliestAllowedLocal)
                        continue;

                    var slotUtc = ToUtc(slotLocal);
                    var slotEndUtc = ComputeEndUtc(slotUtc, settings);
                    if (slotEndUtc <= slotUtc)
                        continue;

                    var overlaps = reservations.Any(x => slotUtc < x.EndAt && slotEndUtc > x.StartAt);
                    if (!overlaps)
                        slots.Add(slotLocal.ToString("HH:mm"));
                }
            }

            return slots.Distinct().OrderBy(x => x).ToList();
        }

        private static void ValidateRequestedStart(RestaurantSettings settings, DateTime requestedStartLocal)
        {
            if (requestedStartLocal <= DateTime.Now)
                throw new InvalidOperationException("Reservation must be in the future.");

            if (requestedStartLocal.Minute % SlotMinutes != 0 || requestedStartLocal.Second != 0)
                throw new InvalidOperationException("Reservations must start at 15 minute intervals.");

            var minuteOfDay = (requestedStartLocal.Hour * 60) + requestedStartLocal.Minute;
            if (!IsMinuteAllowed(settings, minuteOfDay))
                throw new InvalidOperationException("Selected time is outside reservation hours.");
        }

        private static DateTime ComputeEndUtc(DateTime startUtc, RestaurantSettings settings)
        {
            var startLocal = ToLocalTime(startUtc);
            if (!settings.ReservationHoldsTableUntilClose)
                return startUtc.AddMinutes(settings.DefaultReservationDurationMinutes);

            var closeLocal = startLocal.Date.AddMinutes(settings.ReservationLastStartMinuteOfDay);
            if (IsOvernightWindow(settings) && ((startLocal.Hour * 60) + startLocal.Minute) >= settings.ReservationStartMinuteOfDay)
            {
                closeLocal = closeLocal.AddDays(1);
            }
            return ToUtc(closeLocal);
        }

        private static ReservationDto MapReservation(Reservation reservation, string? restaurantName = null, string? tableLabel = null) => new()
        {
            Id = reservation.Id,
            RestaurantId = reservation.RestaurantId,
            RestaurantName = restaurantName ?? reservation.Restaurant?.Name,
            RestaurantTableId = reservation.RestaurantTableId,
            TableLabel = tableLabel ?? reservation.RestaurantTable?.Label,
            CustomerId = reservation.CustomerId,
            GuestName = reservation.GuestName,
            GuestEmail = reservation.GuestEmail,
            GuestPhone = reservation.GuestPhone,
            PartySize = reservation.PartySize,
            StartAt = reservation.StartAt,
            EndAt = reservation.EndAt,
            Status = reservation.Status,
            CancelledAt = reservation.CancelledAt,
            ReleasedAt = reservation.ReleasedAt,
            Note = reservation.Note,
            CreatedAt = reservation.CreatedAt
        };

        private static int ParseMinuteOfDay(string value, string label)
        {
            if (!TimeSpan.TryParseExact(value?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var time))
                throw new InvalidOperationException($"Invalid {label}.");

            return (int)time.TotalMinutes;
        }

        private static (DateTime StartUtc, DateTime EndUtc) GetDayWindowUtc(DateTime localDate)
        {
            var startLocal = localDate.Date;
            var endLocal = startLocal.AddDays(1);
            return (ToUtc(startLocal), ToUtc(endLocal));
        }

        private static DateTime NormalizeIncomingUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value;
            if (value.Kind == DateTimeKind.Local) return value.ToUniversalTime();
            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();
        }

        private static DateTime NormalizeDate(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc) return value.ToLocalTime().Date;
            if (value.Kind == DateTimeKind.Local) return value.Date;
            return DateTime.SpecifyKind(value, DateTimeKind.Local).Date;
        }

        private static DateTime ToLocalTime(DateTime utcValue)
        {
            if (utcValue.Kind == DateTimeKind.Local) return utcValue;
            var normalizedUtc = utcValue.Kind == DateTimeKind.Utc ? utcValue : DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, TimeZoneInfo.Local);
        }

        private static DateTime ToUtc(DateTime localValue)
        {
            if (localValue.Kind == DateTimeKind.Utc) return localValue;
            var normalizedLocal = localValue.Kind == DateTimeKind.Local ? localValue : DateTime.SpecifyKind(localValue, DateTimeKind.Local);
            return normalizedLocal.ToUniversalTime();
        }

        private static string ToLocalTimeString(DateTime utcValue) => ToLocalTime(utcValue).ToString("HH:mm");

        private static DateTime RoundUpToNextSlot(DateTime value)
        {
            var rounded = new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, value.Kind);
            var remainder = rounded.Minute % SlotMinutes;
            if (remainder != 0 || value.Second > 0 || value.Millisecond > 0)
            {
                rounded = rounded.AddMinutes(SlotMinutes - remainder);
            }

            return rounded;
        }

        private static bool IsOvernightWindow(RestaurantSettings settings)
            => settings.ReservationLastStartMinuteOfDay < settings.ReservationStartMinuteOfDay;

        private static bool IsMinuteAllowed(RestaurantSettings settings, int minuteOfDay)
        {
            if (!IsOvernightWindow(settings))
                return minuteOfDay >= settings.ReservationStartMinuteOfDay && minuteOfDay <= settings.ReservationLastStartMinuteOfDay;

            return minuteOfDay >= settings.ReservationStartMinuteOfDay || minuteOfDay <= settings.ReservationLastStartMinuteOfDay;
        }

        private static List<(int StartMinuteOfDay, int EndMinuteOfDay)> BuildScheduleSegments(int startMinuteOfDay, int endMinuteOfDay, int intervalMinutes)
        {
            if (startMinuteOfDay == endMinuteOfDay)
            {
                return [ (startMinuteOfDay, endMinuteOfDay) ];
            }

            if (endMinuteOfDay > startMinuteOfDay)
            {
                return [ (startMinuteOfDay, endMinuteOfDay) ];
            }

            var lastMinuteBeforeMidnight = (24 * 60) - intervalMinutes;
            return
            [
                (startMinuteOfDay, Math.Max(startMinuteOfDay, lastMinuteBeforeMidnight)),
                (0, endMinuteOfDay)
            ];
        }

        private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string BuildNoShowHtml(Reservation reservation)
        {
            var startLocal = ToLocalTime(reservation.StartAt);
            return $@"
                <p>Your reservation was marked as no-show.</p>
                <p><strong>Reservation:</strong> #{reservation.Id}</p>
                <p><strong>Start:</strong> {startLocal:yyyy-MM-dd HH:mm}</p>
            ";
        }
    }
}
