using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data.Entities
{
    public class Order
    {
        public int Id { get; set; }

        // For multi-location later (Must you marked as S, but add now cheaply)
        public int? RestaurantId { get; set; }

        // Guest orders allowed -> nullable
        public string? CustomerId { get; set; }

        public OrderType OrderType { get; set; } = OrderType.Table;

        // For table orders
        public string? TableNumber { get; set; }

        // Unified main lifecycle status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Only used if OrderType == Delivery
        public DeliveryState? DeliveryState { get; set; }

        // Scheduling (your “pay for a specific hour” use case)
        public DateTime? ScheduledFor { get; set; }

        // Payment choice MUST
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.AtCounter;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        // Totals (MUST for receipt + email)
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }

        // Estimation MUST
        public int? EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }

        // Receipt
        public string? ReceiptEmail { get; set; }
        public DateTime? ReceiptSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
