using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.AdminOrders
{
    public class AdminOrderDetailsDto
    {
        public int Id { get; set; }
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }

        public int? EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<AdminOrderLineDto> Items { get; set; } = new();
    }
}
