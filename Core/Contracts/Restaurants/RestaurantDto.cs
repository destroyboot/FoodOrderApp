namespace Core.Contracts.Restaurants
{
    public class RestaurantDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? HouseNumber { get; set; }
        public string? CuisineType { get; set; }
        public List<string> CuisineTypes { get; set; } = new();
        public bool IsActive { get; set; }
        public bool EnableTableOrders { get; set; }
        public bool EnableTakeawayOrders { get; set; }
        public bool EnableDeliveryOrders { get; set; }
        public bool EnableReservations { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryRadiusKm { get; set; }
        public decimal MinimumDeliveryOrder { get; set; }
        public int DeliveryStartMinuteOfDay { get; set; }
        public int DeliveryEndMinuteOfDay { get; set; }
        public int DeliveryLeadTimeMinutes { get; set; }
    }
}
