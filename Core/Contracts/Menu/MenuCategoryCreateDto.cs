using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuCategoryCreateDto
    {
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public List<MenuCategoryTranslationDto> Translations { get; set; } = new();
    }
}
