using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuCategoryDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        public List<MenuCategoryTranslationDto> Translations { get; set; } = new();
    }
}
