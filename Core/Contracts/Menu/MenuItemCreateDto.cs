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
        public decimal CurrentPrice { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; } = true;
        public int? PhotoAssetId { get; set; }
        public string? PhotoPath { get; set; }
        public List<MenuItemTranslationDto> Translations { get; set; } = new();
    }
}
