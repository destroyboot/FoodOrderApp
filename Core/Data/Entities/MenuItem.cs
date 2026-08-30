using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class MenuItem
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public int SortOrder { get; set; } = 0;
        public decimal CurrentPrice { get; set; }

        public bool IsAvailable { get; set; } = true;
        public bool EnableIngredientSwap { get; set; } = false;
        public int? PhotoAssetId { get; set; }
        public string? PhotoPath { get; set; }

        public MenuCategory? Category { get; set; }

        public ICollection<MenuItemTranslation> Translations { get; set; }
            = new List<MenuItemTranslation>();

        public ICollection<MenuItemIngredient> Ingredients { get; set; }
            = new List<MenuItemIngredient>();
    }
}
