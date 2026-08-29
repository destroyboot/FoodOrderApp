namespace Core.Data.Entities
{
    public class ReservationSlot
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int RestaurantTableId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Restaurant? Restaurant { get; set; }
        public RestaurantTable? RestaurantTable { get; set; }
    }
}
