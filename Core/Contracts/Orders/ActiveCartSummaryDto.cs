namespace Core.Contracts.Orders
{
    public class ActiveCartSummaryDto
    {
        public int CartId { get; set; }
        public int? RestaurantId { get; set; }
        public string? RestaurantName { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
