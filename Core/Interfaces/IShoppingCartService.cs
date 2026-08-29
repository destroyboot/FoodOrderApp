using Core.Contracts.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IShoppingCartService
    {
        Task<CartCreateResponseDto> CreateCartAsync(string? customerId, string? guestToken, int? restaurantId = null, CancellationToken ct = default);

        Task<IReadOnlyList<ActiveCartSummaryDto>> GetActiveCartsAsync(string? customerId, string? guestToken, CancellationToken ct = default);

        Task<CartResponseDto> GetCartAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default);

        Task SetMetaAsync(int cartId, CartSetMetaDto dto, string? customerId, string? guestToken, CancellationToken ct = default);

        Task UpdateItemsAsync(int cartId, CartUpdateItemsDto dto, string? customerId, string? guestToken, CancellationToken ct = default);

        Task ClearAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default);

        Task DeleteAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default);

        Task<CartPreviewResponseDto> PreviewAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default);

        Task<FinalizeResponseDto> FinalizeAsync(int cartId, string? customerId, string? guestToken, CancellationToken ct = default);
    }
}
