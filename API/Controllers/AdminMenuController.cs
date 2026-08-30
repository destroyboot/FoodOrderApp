using Core.Contracts.Menu;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace API.Controllers
{
    [Authorize(Roles = "Admin,RestaurantAdmin")]
    [ApiController]
    [Route("api/admin/menu")]
    public class AdminMenuController : ControllerBase
    {
        public class UploadItemPhotoRequest
        {
            public IFormFile File { get; set; } = default!;
        }

        private readonly IMenuService _menu;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AdminMenuController> _logger;

        public AdminMenuController(
            IMenuService menu,
            AppDbContext db,
            IWebHostEnvironment env,
            ILogger<AdminMenuController> logger)
        {
            _menu = menu;
            _db = db;
            _env = env;
            _logger = logger;
        }

        private string UserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Usaer id is missing");

        private bool IsMasterAdmin => User.IsInRole("Admin");

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories([FromQuery] string? culture, [FromQuery] int? restaurantId, CancellationToken ct)
        {
            var allowedRestaurantIds = await GetReadableRestaurantIdsAsync(restaurantId, ct);
            var result = await _menu.GetCategoriesAsync(culture, restaurantId, ct);
            if (allowedRestaurantIds is not null)
                result = result.Where(x => allowedRestaurantIds.Contains(x.RestaurantId)).ToList();
            return Ok(result);
        }

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory(MenuCategoryCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(dto.RestaurantId, ct);
            var id = await _menu.CreateCategoryAsync(dto, userId: UserId, ct);
            return CreatedAtAction(nameof(GetCategory), new { id }, new { id });
        }

        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, MenuCategoryUpdateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetCategoryRestaurantIdAsync(id, ct), ct);
            await EnsureCanManageRestaurantAsync(dto.RestaurantId, ct);
            await _menu.UpdateCategoryAsync(id, dto, userId: UserId, ct);
            return NoContent();
        }

        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetCategoryRestaurantIdAsync(id, ct), ct);
            await _menu.DeleteCategoryAsync(id, userId: UserId, ct);
            return NoContent();
        }

        [HttpGet("categories/{id:int}")]
        public async Task<IActionResult> GetCategory(int id, CancellationToken ct)
        {
            var category = await _menu.GetCategoryByIdAsync(id, ct);
            if (category is null) return NotFound();

            await EnsureCanManageRestaurantAsync(category.RestaurantId, ct);
            return Ok(category);
        }

        [HttpPut("categories/{id:int}/translations")]
        public async Task<IActionResult> UpsertCategoryTranslation(
            int id,
            MenuCategoryTranslationUpsertDto dto,
            CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetCategoryRestaurantIdAsync(id, ct), ct);
            await _menu.UpsertCategoryTranslationAsync(id, dto, userId: UserId, ct);
            return NoContent();
        }

        [HttpPost("items")]
        public async Task<IActionResult> CreateItem(MenuItemCreateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetCategoryRestaurantIdAsync(dto.MenuCategoryId, ct), ct);
            var id = await _menu.CreateItemAsync(dto, userId: UserId, ct);
            return CreatedAtAction(nameof(GetItem), new { id }, new { id });
        }

        [HttpPost("items/photo")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadItemPhoto([FromForm] UploadItemPhotoRequest request, CancellationToken ct)
        {
            var file = request.File;
            if (file is null || file.Length == 0)
                return BadRequest(new { error = "Photo file is required." });

            var extension = Path.GetExtension(file.FileName);
            var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg", ".jpeg", ".png", ".webp"
            };

            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { error = "Only JPG, PNG, and WEBP images are allowed." });

            var uploadsRoot = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads", "menu-items");

            var safeBaseName = Regex.Replace(Path.GetFileNameWithoutExtension(file.FileName), "[^a-zA-Z0-9_-]+", "-").Trim('-');
            if (string.IsNullOrWhiteSpace(safeBaseName))
                safeBaseName = "menu-item";

            var fileName = $"{safeBaseName}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var absolutePath = Path.Combine(uploadsRoot, fileName);

            try
            {
                Directory.CreateDirectory(uploadsRoot);
                await using var stream = System.IO.File.Create(absolutePath);
                await file.CopyToAsync(stream, ct);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Could not save uploaded menu photo {FileName}", file.FileName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Could not save photo." });
            }

            var relativePath = $"/uploads/menu-items/{fileName}";
            return Ok(new
            {
                photoPath = relativePath,
                photoUrl = _menu.BuildPhotoUrl(relativePath),
                fileName
            });
        }

        [HttpGet("items")]
        public async Task<IActionResult> GetItems([FromQuery] string? culture, [FromQuery] int? restaurantId, CancellationToken ct)
        {
            var allowedRestaurantIds = await GetReadableRestaurantIdsAsync(restaurantId, ct);
            var result = await _menu.GetItemsAsync(culture, restaurantId, ct);
            if (allowedRestaurantIds is not null)
                result = result.Where(x => allowedRestaurantIds.Contains(x.RestaurantId)).ToList();
            return Ok(result);
        }

        [HttpGet("items/{id:int}")]
        public async Task<IActionResult> GetItem(int id, [FromQuery] string? culture, CancellationToken ct)
        {
            var item = await _menu.GetItemByIdAsync(id, culture, ct);
            if (item is null)
                return NotFound();

            await EnsureCanManageRestaurantAsync(item.RestaurantId, ct);
            return Ok(item);
        }

        [HttpPut("items/{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, MenuItemUpdateDto dto, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetItemRestaurantIdAsync(id, ct), ct);
            await EnsureCanManageRestaurantAsync(await GetCategoryRestaurantIdAsync(dto.MenuCategoryId, ct), ct);
            await _menu.UpdateItemAsync(id, dto, userId: UserId, ct);
            return NoContent();
        }

        [HttpDelete("items/{id:int}")]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetItemRestaurantIdAsync(id, ct), ct);
            await _menu.DeleteItemAsync(id, userId: UserId, ct);
            return NoContent();
        }

        [HttpPatch("items/{id:int}/availability")]
        public async Task<IActionResult> SetAvailability(int id, [FromQuery] bool isAvailable, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetItemRestaurantIdAsync(id, ct), ct);
            await _menu.SetAvailabilityAsync(id, isAvailable, userId: UserId, ct);
            return NoContent();
        }

        [HttpPatch("items/{id:int}/price")]
        public async Task<IActionResult> ChangePrice(int id, [FromQuery] decimal newPrice, CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetItemRestaurantIdAsync(id, ct), ct);
            await _menu.ChangePriceAsync(id, newPrice, userId: UserId, ct);
            return NoContent();
        }

        [HttpPut("items/{id:int}/translations")]
        public async Task<IActionResult> UpsertItemTranslation(
            int id,
            MenuItemTranslationUpsertDto dto,
            CancellationToken ct)
        {
            await EnsureCanManageRestaurantAsync(await GetItemRestaurantIdAsync(id, ct), ct);
            await _menu.UpsertItemTranslationAsync(id, dto, userId: UserId, ct);
            return NoContent();
        }

        private async Task<List<int>?> GetReadableRestaurantIdsAsync(int? restaurantId, CancellationToken ct)
        {
            if (IsMasterAdmin)
                return restaurantId.HasValue ? new List<int> { restaurantId.Value } : null;

            var assignedRestaurantIds = await _db.RestaurantUserRoles
                .Where(x => x.UserId == UserId && x.Role == "RestaurantAdmin")
                .Select(x => x.RestaurantId)
                .Distinct()
                .ToListAsync(ct);

            if (assignedRestaurantIds.Count == 0)
                throw new InvalidOperationException("You are not assigned to a restaurant.");

            if (restaurantId.HasValue)
            {
                if (!assignedRestaurantIds.Contains(restaurantId.Value))
                    throw new InvalidOperationException("You are not allowed to manage this restaurant.");

                return new List<int> { restaurantId.Value };
            }

            return assignedRestaurantIds;
        }

        private async Task EnsureCanManageRestaurantAsync(int restaurantId, CancellationToken ct)
        {
            if (restaurantId <= 0)
                throw new InvalidOperationException("Restaurant is required.");

            var exists = await _db.Restaurants.AnyAsync(r => r.Id == restaurantId, ct);
            if (!exists)
                throw new KeyNotFoundException("Restaurant not found.");

            if (IsMasterAdmin)
                return;

            var canManage = await _db.RestaurantUserRoles.AnyAsync(
                x => x.RestaurantId == restaurantId && x.UserId == UserId && x.Role == "RestaurantAdmin",
                ct);

            if (!canManage)
                throw new InvalidOperationException("You are not allowed to manage this restaurant.");
        }

        private async Task<int> GetCategoryRestaurantIdAsync(int categoryId, CancellationToken ct)
        {
            var restaurantId = await _db.MenuCategories
                .Where(c => c.Id == categoryId)
                .Select(c => (int?)c.RestaurantId)
                .FirstOrDefaultAsync(ct);

            return restaurantId ?? throw new KeyNotFoundException("Category not found.");
        }

        private async Task<int> GetItemRestaurantIdAsync(int itemId, CancellationToken ct)
        {
            var restaurantId = await _db.MenuItems
                .Where(i => i.Id == itemId)
                .Select(i => (int?)i.Category!.RestaurantId)
                .FirstOrDefaultAsync(ct);

            return restaurantId ?? throw new KeyNotFoundException("Menu item not found.");
        }
    }
}
