using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.AdminOrders
{
    public class AdminOrderListItemDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }

        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ItemCount { get; set; }
    }
}
