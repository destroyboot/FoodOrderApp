using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuItemCreateDto
    {
        public int MenuCategoryId { get; set; }

        // for now: current price (later you can add history automatically)
        public decimal CurrentPrice { get; set; }

        public bool IsAvailable { get; set; } = true;

        // Optional - link to uploaded Asset (you’ll implement assets later)
        public int? PhotoAssetId { get; set; }

        // Must contain at least default language translation (you enforce in service)
        public List<MenuItemTranslationDto> Translations { get; set; } = new();
    }
}
