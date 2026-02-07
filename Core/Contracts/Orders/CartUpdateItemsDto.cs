using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class CartUpdateItemsDto
    {
        public List<CartItemDto> Items { get; set; } = new();
    }
}
