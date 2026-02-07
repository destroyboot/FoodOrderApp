using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class MenuItemTranslation
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }

        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        // For product page MUST: allergens
        public string? Allergens { get; set; }
    }
}
