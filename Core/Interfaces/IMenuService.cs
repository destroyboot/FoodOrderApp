using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Contracts.Menu;

namespace Core.Interfaces
{
    public interface IMenuService
    {
        //Cat
        Task<int> CreateCategoryAsync(MenuCategoryCreateDto dto, string userId, CancellationToken ct = default);
        Task UpdateCategoryAsync(int id, MenuCategoryUpdateDto dto, string userId, CancellationToken ct = default);
        Task DeleteCategoryAsync(int id, string userId, CancellationToken ct = default);
        Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(CancellationToken ct = default);
        Task<MenuCategoryDto?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
        Task UpsertCategoryTranslationAsync(int categoryId, MenuCategoryTranslationUpsertDto dto, string userId, CancellationToken ct = default);

        //Items
        Task<int> CreateItemAsync(MenuItemCreateDto dto, string userId, CancellationToken ct = default);
        Task UpdateItemAsync(int id, MenuItemUpdateDto dto, string userId, CancellationToken ct = default);
        Task DeleteItemAsync(int id, string userId, CancellationToken ct = default);
        Task<IReadOnlyList<MenuItemDto>> GetItemsAsync(string? culture, CancellationToken ct = default);
        Task<MenuItemDto?> GetItemByIdAsync(int id, string? culture, CancellationToken ct = default);
        Task UpsertItemTranslationAsync(int itemId, MenuItemTranslationUpsertDto dto, string userId, CancellationToken ct = default);

        //Other
        Task ChangePriceAsync(int id, decimal newPrice, string userId, CancellationToken ct = default);
        Task SetAvailabilityAsync(int id, bool isAvailable, string userId, CancellationToken ct = default);
    }
}
