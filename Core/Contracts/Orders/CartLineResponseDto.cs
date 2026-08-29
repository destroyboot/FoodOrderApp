using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class CartLineResponseDto
    {
        public int LineId { get; set; }
        public int MenuItemId { get; set; }
        public string? MenuItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public string? Note { get; set; }
        public decimal ExtraCharge { get; set; }
        public List<int> RemovedIngredientIds { get; set; } = new();
        public List<int> AddedIngredientIds { get; set; } = new();
    }
}
