using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Menu
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public int MenuCategoryId { get; set; }
        public decimal CurrentPrice { get; set; }
        public bool IsAvailable { get; set; }
        public int? PhotoAssetId { get; set; }

        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
}
