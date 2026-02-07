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

        // Current price (you can add history later)
        public decimal CurrentPrice { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Optional reference to uploaded photo (you’ll add Asset entity later)
        public int? PhotoAssetId { get; set; }

        public MenuCategory? Category { get; set; }

        public ICollection<MenuItemTranslation> Translations { get; set; }
            = new List<MenuItemTranslation>();
    }
}
