using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuCategoryUpdateDto
    {
        public int RestaurantId { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }

        public List<MenuCategoryTranslationDto> Translations { get; set; } = new();
    }
}
