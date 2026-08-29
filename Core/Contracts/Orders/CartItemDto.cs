using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class CartItemDto
    {
        public int? LineId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public string? Note { get; set; }
        public List<int> RemovedIngredientIds { get; set; } = new();
        public List<int> AddedIngredientIds { get; set; } = new();
    }
}
