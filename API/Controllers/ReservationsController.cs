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
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsController : ControllerBase
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

        public ReservationsController(
            AppDbContext db,
            UserManager<ApplicationUser> users,
            IEmailSender email)
        {
            _db = db;
            _users = users;
            _email = email;
        }

        [HttpGet("availability")]
        [Authorize]
        public async Task<ActionResult<ReservationAvailabilityDto>> GetAvailability(
            [FromQuery] int restaurantId,
            [FromQuery] DateTime date,
            CancellationToken ct)
        {
            return Ok(await BuildAvailabilityAsync(restaurantId, date, ct));
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ReservationDto>> Create(ReservationCreateDto dto, CancellationToken ct)
        {
            var user = await GetActiveUserAsync(ct);
            var restaurant = await GetRestaurantWithSettingsAsync(dto.RestaurantId, ct);
            if (restaurant is null)
                return NotFound();

            var settings = restaurant.Settings ?? new RestaurantSettings { RestaurantId = restaurant.Id };
            if (!settings.EnableReservations)
                throw new InvalidOperationException("Reservations are not enabled for this restaurant.");

            await AutoReleaseExpiredReservationsAsync(dto.RestaurantId, ct);

            var requestedStartUtc = NormalizeIncomingUtc(dto.StartAt);
            var requestedStartLocal = ToLocalTime(requestedStartUtc);
            var endUtc = ComputeEndUtc(requestedStartUtc, settings);
            var partySize = Math.Max(1, dto.PartySize);

            RestaurantTable? table;
            if (settings.AllowUserTableSelectionForReservations)
            {
                if (!dto.RestaurantTableId.HasValue)
                    throw new InvalidOperationException("Table selection is required.");

                table = await _db.RestaurantTables
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.RestaurantTableId.Value &&
                        x.RestaurantId == dto.RestaurantId &&
                        x.IsActive &&
                        x.IsReservable,
                        ct);

                if (table is null)
                    throw new InvalidOperationException("Selected table is not available for reservations.");

                if (table.Seats.HasValue && partySize > table.Seats.Value)
                    throw new InvalidOperationException("Selected table does not have enough seats.");

                await EnsureReservationAllowedByScheduleAsync(dto.RestaurantId, table.Id, requestedStartLocal, settings, ct);
                await EnsureNoOverlapAsync(table.Id, requestedStartUtc, endUtc, ct);
            }
            else
            {
                table = await FindFirstAvailableTableAsync(dto.RestaurantId, requestedStartLocal, requestedStartUtc, endUtc, partySize, settings, ct);
                if (table is null)
                    throw new InvalidOperationException("No reservation slot is available for this party size at the selected time.");
            }

            var reservation = new Reservation
            {
                RestaurantId = dto.RestaurantId,
                RestaurantTableId = table.Id,
                CustomerId = user.Id,
                GuestName = user.UserName ?? user.Email ?? $"user-{user.Id}",
                GuestEmail = user.Email,
                GuestPhone = user.PhoneNumber,
                PartySize = partySize,
                StartAt = requestedStartUtc,
                EndAt = endUtc,
                Note = TrimOrNull(dto.Note),
                Status = ReservationStatus.Requested
            };

            _db.Reservations.Add(reservation);
            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                await _email.SendAsync(
                    user.Email,
                    $"Food Order App - Reservation requested for {restaurant.Name}",
                    BuildReservationRequestedHtml(restaurant.Name, table.Label, reservation.StartAt, reservation.EndAt, reservation.Note),
                    ct: ct);
            }

            return CreatedAtAction(nameof(GetMine), new { id = reservation.Id }, Map(reservation, restaurant.Name, table.Label));
        }

        [HttpGet("mine")]
        [Authorize]
        public async Task<ActionResult<IReadOnlyList<ReservationDto>>> GetMine(CancellationToken ct)
        {
            var user = await GetActiveUserAsync(ct);
            await AutoReleaseExpiredReservationsAsync(null, ct);

            var result = await _db.Reservations
                .AsNoTracking()
                .Include(x => x.Restaurant)
                .Include(x => x.RestaurantTable)
                .Where(x => x.CustomerId == user.Id)
                .OrderByDescending(x => x.StartAt)
                .ToListAsync(ct);

            return Ok(result.Select(x => Map(x, x.Restaurant?.Name, x.RestaurantTable?.Label)).ToList());
        }

        [HttpPatch("{id:int}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelMine(int id, CancellationToken ct)
        {
            var user = await GetActiveUserAsync(ct);
            var reservation = await _db.Reservations
                .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == user.Id, ct);

            if (reservation is null)
                return NotFound();

            if (reservation.Status is ReservationStatus.Cancelled or ReservationStatus.Completed or ReservationStatus.NoShow)
                throw new InvalidOperationException("This reservation cannot be cancelled.");

            reservation.Status = ReservationStatus.Cancelled;
            reservation.CancelledAt = DateTime.UtcNow;
            reservation.ReleasedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            return NoContent();
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
                Tables = tables
                    .Where(table => settings.AllowUserTableSelectionForReservations || table.Seats is null || table.Seats.Value > 0)
                    .Select(table =>
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

        private async Task<RestaurantTable?> FindFirstAvailableTableAsync(
            int restaurantId,
            DateTime requestedStartLocal,
            DateTime requestedStartUtc,
            DateTime endUtc,
            int partySize,
            RestaurantSettings settings,
            CancellationToken ct)
        {
            var tables = await _db.RestaurantTables
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId && x.IsActive && x.IsReservable)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Label)
                .ToListAsync(ct);

            foreach (var table in tables.Where(x => !x.Seats.HasValue || x.Seats.Value >= partySize))
            {
                try
                {
                    await EnsureReservationAllowedByScheduleAsync(restaurantId, table.Id, requestedStartLocal, settings, ct);
                    await EnsureNoOverlapAsync(table.Id, requestedStartUtc, endUtc, ct);
                    return table;
                }
                catch (InvalidOperationException)
                {
                }
            }

            return null;
        }

        private async Task EnsureReservationAllowedByScheduleAsync(
            int restaurantId,
            int tableId,
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

        private async Task<ApplicationUser> GetActiveUserAsync(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");

            var user = await _users.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
                ?? throw new InvalidOperationException("User not found.");

            if (!user.EmailConfirmed)
                throw new InvalidOperationException("Only active accounts can make reservations.");

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                throw new InvalidOperationException("This account is currently locked.");

            return user;
        }

        private async Task<Restaurant?> GetRestaurantWithSettingsAsync(int restaurantId, CancellationToken ct)
        {
            return await _db.Restaurants
                .Include(x => x.Settings)
                .FirstOrDefaultAsync(x => x.Id == restaurantId && x.IsActive, ct);
        }

        private async Task AutoReleaseExpiredReservationsAsync(int? restaurantId, CancellationToken ct)
        {
            var nowUtc = DateTime.UtcNow;
            var query = _db.Reservations.Where(x => AutoReleaseStatuses.Contains(x.Status));

            if (restaurantId.HasValue)
                query = query.Where(x => x.RestaurantId == restaurantId.Value);

            var reservations = await query
                .Include(x => x.Restaurant)
                .ThenInclude(x => x!.Settings)
                .ToListAsync(ct);

            var changed = false;
            foreach (var reservation in reservations)
            {
                var settings = reservation.Restaurant?.Settings ?? new RestaurantSettings { RestaurantId = reservation.RestaurantId };
                var grace = settings.ReservationGracePeriodMinutes < 0 ? 0 : settings.ReservationGracePeriodMinutes;
                var expiresAt = reservation.StartAt.AddMinutes(grace);

                if (expiresAt < nowUtc)
                {
                    reservation.Status = ReservationStatus.NoShow;
                    reservation.ReleasedAt ??= nowUtc;
                    changed = true;
                }
            }

            if (changed)
                await _db.SaveChangesAsync(ct);
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

        private static ReservationDto Map(Reservation reservation, string? restaurantName, string? tableLabel) => new()
        {
            Id = reservation.Id,
            RestaurantId = reservation.RestaurantId,
            RestaurantName = restaurantName,
            RestaurantTableId = reservation.RestaurantTableId,
            TableLabel = tableLabel,
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

        private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string BuildReservationRequestedHtml(
            string restaurantName,
            string? tableLabel,
            DateTime startAtUtc,
            DateTime endAtUtc,
            string? note)
        {
            var startLocal = ToLocalTime(startAtUtc);
            var endLocal = ToLocalTime(endAtUtc);
            var noteBlock = string.IsNullOrWhiteSpace(note) ? "" : $"<p>Note: {System.Net.WebUtility.HtmlEncode(note)}</p>";
            var tableBlock = string.IsNullOrWhiteSpace(tableLabel)
                ? ""
                : $"<p><strong>Table:</strong> {System.Net.WebUtility.HtmlEncode(tableLabel)}</p>";

            return $@"
                <p>Your reservation request has been sent.</p>
                <p><strong>Restaurant:</strong> {System.Net.WebUtility.HtmlEncode(restaurantName)}</p>
                {tableBlock}
                <p><strong>Start:</strong> {startLocal:yyyy-MM-dd HH:mm}</p>
                <p><strong>End:</strong> {endLocal:yyyy-MM-dd HH:mm}</p>
                {noteBlock}
            ";
        }
    }
}
