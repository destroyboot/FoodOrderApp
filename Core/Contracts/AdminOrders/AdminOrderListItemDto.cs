using Core.Data.Enums;

namespace Core.Contracts.AdminOrders
{
    public class AdminOrderListItemDto
    {
        public int Id { get; set; }
        public string DisplayOrderNumber { get; set; } = "";
        public OrderStatus Status { get; set; }
        public OrderType OrderType { get; set; }
        public string? TableNumber { get; set; }
        public string? PickupContactName { get; set; }
        public string? PickupPhone { get; set; }
        public string? DeliveryContactName { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryAddressLine1 { get; set; }
        public string? DeliveryCity { get; set; }
        public string? AssignedDeliveryDriverUserId { get; set; }
        public string? AssignedDeliveryDriverName { get; set; }
        public string? CustomerUserId { get; set; }
        public string? CustomerEmail { get; set; }
        public bool IsAnonymousCustomer { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public string? ReceiptEmail { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public int? RestaurantId { get; set; }
        public int? ReservationId { get; set; }

        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }

        public int ItemCount { get; set; }
    }
}
