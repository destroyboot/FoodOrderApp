using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Contracts.Orders
{
    public class OrderPreviewRequestDto
    {
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }
        public int? RestaurantId { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        // For “scheduled for specific hour” (future)
        public DateTime? ScheduledFor { get; set; }

        // If user wants receipt emailed
        public string? ReceiptEmail { get; set; }

        public List<OrderItemRequestDto> Items { get; set; } = new();
    }
}
