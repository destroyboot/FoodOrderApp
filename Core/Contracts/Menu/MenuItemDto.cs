using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public int RestaurantId { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int SortOrder { get; set; }
        public bool EnableIngredientSwap { get; set; }
        public int? PhotoAssetId { get; set; }
        public string? PhotoPath { get; set; }
        public string? PhotoUrl { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string? Allergens { get; set; }

        public List<MenuItemTranslationDto> Translations { get; set; } = new();
    }
}
