using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class CartSetMetaDto
    {
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }
        public int? RestaurantId { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public string? ReceiptEmail { get; set; }
    }
}
