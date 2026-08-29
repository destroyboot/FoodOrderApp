using Core.Contracts.Reservations;
using Core.Contracts.Restaurants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/tables")]
    [Authorize(Roles = "Admin,RestaurantAdmin,Waiter")]
    public class AdminTablesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminTablesController(AppDbContext db) => _db = db;

        [HttpGet("restaurants")]
        public async Task<ActionResult<IReadOnlyList<AdminReservationRestaurantDto>>> GetRestaurants(CancellationToken ct)
        {
            var allowedIds = await GetAllowedRestaurantIdsAsync(ct);

            var query = _db.Restaurants
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name);

            if (allowedIds != null)
            {
                query = (IOrderedQueryable<Core.Data.Entities.Restaurant>)query.Where(x => allowedIds.Contains(x.Id));
            }

            var result = await query
                .Select(x => new AdminReservationRestaurantDto
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToListAsync(ct);

            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<RestaurantTableDto>>> GetTables([FromQuery] int restaurantId, CancellationToken ct)
        {
            await EnsureCanManageTablesAsync(restaurantId, ct);

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

        [HttpPost]
        public async Task<IActionResult> CreateTable(RestaurantTableDto dto, CancellationToken ct)
        {
            await EnsureCanManageTablesAsync(dto.RestaurantId, ct);

            var label = dto.Label?.Trim();
            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("Table label is required.");

            _db.RestaurantTables.Add(new Core.Data.Entities.RestaurantTable
            {
                RestaurantId = dto.RestaurantId,
                Label = label,
                Seats = dto.Seats,
                IsActive = dto.IsActive,
                IsReservable = dto.IsReservable,
                SortOrder = dto.SortOrder
            });

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpPut("{tableId:int}")]
        public async Task<IActionResult> UpdateTable(int tableId, RestaurantTableDto dto, CancellationToken ct)
        {
            await EnsureCanManageTablesAsync(dto.RestaurantId, ct);

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == dto.RestaurantId, ct);

            if (table is null) return NotFound();

            var label = dto.Label?.Trim();
            if (string.IsNullOrWhiteSpace(label))
                throw new InvalidOperationException("Table label is required.");

            table.Label = label;
            table.Seats = dto.Seats;
            table.IsActive = dto.IsActive;
            table.IsReservable = dto.IsReservable;
            table.SortOrder = dto.SortOrder;

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpDelete("{tableId:int}")]
        public async Task<IActionResult> DeleteTable(int tableId, [FromQuery] int restaurantId, CancellationToken ct)
        {
            await EnsureCanManageTablesAsync(restaurantId, ct);

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(t => t.Id == tableId && t.RestaurantId == restaurantId, ct);

            if (table is null) return NotFound();

            _db.RestaurantTables.Remove(table);
            await _db.SaveChangesAsync(ct);
            return NoContent();
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

        private async Task EnsureCanManageTablesAsync(int restaurantId, CancellationToken ct)
        {
            var restaurantExists = await _db.Restaurants.AnyAsync(x => x.Id == restaurantId, ct);
            if (!restaurantExists)
                throw new KeyNotFoundException("Restaurant not found.");

            var ids = await GetAllowedRestaurantIdsAsync(ct);
            if (ids == null || ids.Contains(restaurantId))
                return;

            throw new InvalidOperationException("You are not allowed to manage tables for this restaurant.");
        }
    }
}
