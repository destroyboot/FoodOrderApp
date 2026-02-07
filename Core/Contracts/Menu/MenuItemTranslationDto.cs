using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuItemTranslationDto
    {
        // e.g. "pl-PL", "en-US"
        public string Culture { get; set; } = default!;

        public string Name { get; set; } = default!;
        public string? Description { get; set; }

        // keep simple for now; can evolve to structured allergen flags later
        public string? Allergens { get; set; }
    }
}
