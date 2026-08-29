using Core.Contracts.Restaurants;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/restaurants")]
    public class RestaurantsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RestaurantsController(AppDbContext db) => _db = db;

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<RestaurantDto>>> GetRestaurants(CancellationToken ct)
        {
            var result = await _db.Restaurants
                .AsNoTracking()
                .Include(r => r.Settings)
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Address = r.Address,
                    CuisineType = r.CuisineType,
                    IsActive = r.IsActive,
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

            return Ok(result);
        }

        [HttpGet("{restaurantId:int}/tables")]
        [AllowAnonymous]
        public async Task<ActionResult<IReadOnlyList<RestaurantTableDto>>> GetTables(int restaurantId, CancellationToken ct)
        {
            var restaurantExists = await _db.Restaurants
                .AsNoTracking()
                .AnyAsync(r => r.Id == restaurantId && r.IsActive, ct);

            if (!restaurantExists)
                return NotFound();

            var result = await _db.RestaurantTables
                .AsNoTracking()
                .Where(t => t.RestaurantId == restaurantId && t.IsActive)
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

        [HttpGet("{restaurantId:int}/settings")]
        [AllowAnonymous]
        public async Task<ActionResult<RestaurantSettingsDto>> GetSettings(int restaurantId, CancellationToken ct)
        {
            var restaurant = await _db.Restaurants
                .AsNoTracking()
                .Include(r => r.Settings)
                .FirstOrDefaultAsync(r => r.Id == restaurantId && r.IsActive, ct);

            if (restaurant is null)
                return NotFound();

            return Ok(MapSettings(restaurant.Id, restaurant.Settings));
        }

        private static RestaurantSettingsDto MapSettings(int restaurantId, Core.Data.Entities.RestaurantSettings? settings)
        {
            settings ??= new Core.Data.Entities.RestaurantSettings { RestaurantId = restaurantId };

            return new RestaurantSettingsDto
            {
                RestaurantId = restaurantId,
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
        }
    }
}
