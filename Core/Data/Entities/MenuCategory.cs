using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class MenuCategory
    {
        public int Id { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        public ICollection<MenuCategoryTranslation> Translations { get; set; }
            = new List<MenuCategoryTranslation>();
    }
}
