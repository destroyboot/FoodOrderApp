using Core.Contracts.Orders;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly IShoppingCartService _cart;

        public CartController(IShoppingCartService cart) => _cart = cart;

        private string? CustomerId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        private string? GuestToken
            => Request.Headers.TryGetValue("X-Guest-Token", out var v) ? v.ToString() : null;

        // Optional: enforce that at least one owner identifier exists
        private void EnsureOwnerProvided()
        {
            if (string.IsNullOrWhiteSpace(CustomerId) && string.IsNullOrWhiteSpace(GuestToken))
                throw new InvalidOperationException("Provide X-Guest-Token header or authenticate with JWT.");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<CartCreateResponseDto>> Create(CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.CreateCartAsync(CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpGet("{cartId:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CartResponseDto>> Get(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.GetCartAsync(cartId, CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpPut("{cartId:int}/meta")]
        [AllowAnonymous]
        public async Task<IActionResult> SetMeta(int cartId, [FromBody] CartSetMetaDto dto, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.SetMetaAsync(cartId, dto, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpPut("{cartId:int}/items")]
        [AllowAnonymous]
        public async Task<IActionResult> UpdateItems(int cartId, [FromBody] CartUpdateItemsDto dto, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.UpdateItemsAsync(cartId, dto, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpDelete("{cartId:int}/items")]
        [AllowAnonymous]
        public async Task<IActionResult> ClearItems(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            await _cart.ClearAsync(cartId, CustomerId, GuestToken, ct);
            return NoContent();
        }

        [HttpPost("{cartId:int}/preview")]
        [AllowAnonymous]
        public async Task<ActionResult<CartPreviewResponseDto>> Preview(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.PreviewAsync(cartId, CustomerId, GuestToken, ct);
            return Ok(result);
        }

        [HttpPost("{cartId:int}/finalize")]
        [AllowAnonymous]
        public async Task<ActionResult<FinalizeResponseDto>> Finalize(int cartId, CancellationToken ct)
        {
            EnsureOwnerProvided();
            var result = await _cart.FinalizeAsync(cartId, CustomerId, GuestToken, ct);
            return Ok(result);
        }
    }
}
