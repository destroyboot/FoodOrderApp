using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }

        public int Quantity { get; set; }

        // Price snapshot
        public decimal UnitPrice { get; set; }
        public decimal ExtraCharge { get; set; }

        // Optional
        public string? Note { get; set; }
        public string? CustomizationsJson { get; set; }
    }
}
