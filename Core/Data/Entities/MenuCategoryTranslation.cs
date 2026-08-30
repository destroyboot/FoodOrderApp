using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class MenuCategoryTranslation
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
