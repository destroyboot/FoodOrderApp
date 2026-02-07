using Core.Contracts.Menu;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin/menu")]
    public class AdminMenuController : ControllerBase
    {
        private readonly IMenuService _menu;

        public AdminMenuController(IMenuService menu) => _menu = menu;

        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory(MenuCategoryCreateDto dto, CancellationToken ct)
        {
            var id = await _menu.CreateCategoryAsync(dto, userId: "system", ct);
            return CreatedAtAction(nameof(GetCategory), new { id }, new { id });
        }

        [HttpGet("categories/{id:int}")]
        public IActionResult GetCategory(int id) => Ok(new { id }); // optional for now

        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, MenuCategoryUpdateDto dto, CancellationToken ct)
        {
            await _menu.UpdateCategoryAsync(id, dto, userId: "system", ct);
            return NoContent();
        }

        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        {
            await _menu.DeleteCategoryAsync(id, userId: "system", ct);
            return NoContent();
        }

        [HttpPost("items")]
        public async Task<IActionResult> CreateItem(MenuItemCreateDto dto, CancellationToken ct)
        {
            var id = await _menu.CreateItemAsync(dto, userId: "system", ct);
            return CreatedAtAction(nameof(GetItem), new { id }, new { id });
        }

        [HttpGet("items/{id:int}")]
        public IActionResult GetItem(int id) => Ok(new { id }); // optional for now

        [HttpPut("items/{id:int}")]
        public async Task<IActionResult> UpdateItem(int id, MenuItemUpdateDto dto, CancellationToken ct)
        {
            await _menu.UpdateItemAsync(id, dto, userId: "system", ct);
            return NoContent();
        }

        [HttpDelete("items/{id:int}")]
        public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
        {
            await _menu.DeleteItemAsync(id, userId: "system", ct);
            return NoContent();
        }

        [HttpPatch("items/{id:int}/availability")]
        public async Task<IActionResult> SetAvailability(int id, [FromQuery] bool isAvailable, CancellationToken ct)
        {
            await _menu.SetAvailabilityAsync(id, isAvailable, userId: "system", ct);
            return NoContent();
        }

        [HttpPatch("items/{id:int}/price")]
        public async Task<IActionResult> ChangePrice(int id, [FromQuery] decimal newPrice, CancellationToken ct)
        {
            await _menu.ChangePriceAsync(id, newPrice, userId: "system", ct);
            return NoContent();
        }
    }
}
