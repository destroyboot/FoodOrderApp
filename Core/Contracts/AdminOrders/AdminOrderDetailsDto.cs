using Core.Contracts.Orders;
using Core.Data.Enums;

namespace Core.Contracts.AdminOrders
{
    public class AdminOrderDetailsDto
    {
        public int Id { get; set; }
        public string DisplayOrderNumber { get; set; } = "";
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }
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
        public string? AssignedDeliveryDriverName { get; set; }
        public string? CustomerUserId { get; set; }
        public string? CustomerEmail { get; set; }
        public bool IsAnonymousCustomer { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public int? RestaurantId { get; set; }
        public int? ReservationId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? ReceiptEmail { get; set; }
        public DateTime? ReceiptSentAt { get; set; }
        public string? InvoiceNumber { get; set; }
        public bool HasInvoiceDocument { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }

        public int? EstimatedPreparationMinutes { get; set; }
        public DateTime? EstimatedReadyAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public OrderBillingDetailsDto? BillingDetails { get; set; }
        public List<AdminOrderLineDto> Items { get; set; } = new();
        public List<OrderCommentDto> Comments { get; set; } = new();
    }
}
