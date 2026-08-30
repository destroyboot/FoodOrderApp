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
        public int? DailyRestaurantOrderNumber { get; set; }
        public DateOnly? DailyRestaurantOrderDate { get; set; }
        public int? RestaurantId { get; set; }
        public string? CustomerId { get; set; }

        public OrderType OrderType { get; set; } = OrderType.Table;

        public int? RestaurantTableId { get; set; }
        public string? TableNumber { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DeliveryState? DeliveryState { get; set; }

        public DateTime? ScheduledFor { get; set; }
        public int? ReservationId { get; set; }

        public string? PickupContactName { get; set; }
        public string? PickupPhone { get; set; }
        public string? PickupNote { get; set; }

        public string? DeliveryContactName { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryAddressLine1 { get; set; }
        public string? DeliveryAddressLine2 { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? DeliveryCountry { get; set; }
        public string? DeliveryNote { get; set; }
        public string? AssignedDeliveryDriverUserId { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.AtCounter;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }

        public int? EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }

        public string? ReceiptEmail { get; set; }
        public DateTime? ReceiptSentAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Restaurant? Restaurant { get; set; }
        public Reservation? Reservation { get; set; }
        public OrderBillingDetails? BillingDetails { get; set; }
        public OrderInvoiceDocument? InvoiceDocument { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderComment> Comments { get; set; } = new List<OrderComment>();
    }
}
