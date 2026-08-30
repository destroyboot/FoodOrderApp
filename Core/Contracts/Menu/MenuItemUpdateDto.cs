using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuItemUpdateDto
    {
        public int MenuCategoryId { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; }
        public int? PhotoAssetId { get; set; }
        public string? PhotoPath { get; set; }
        public List<MenuItemTranslationDto> Translations { get; set; } = new();
    }
}
