using Core.Data.Enums;

namespace Core.Contracts.Orders
{
    public class CartSetMetaDto
    {
        public OrderType OrderType { get; set; }
        public int? RestaurantTableId { get; set; }
        public string? TableNumber { get; set; }
        public int? RestaurantId { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
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
        public string? ReceiptEmail { get; set; }
        public OrderBillingDetailsDto? BillingDetails { get; set; }
    }
}
