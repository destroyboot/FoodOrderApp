using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuCategoryTranslationDto
    {
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
