namespace Core.Data.Entities
{
    public class ReservationSchedule
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int RestaurantTableId { get; set; }
        public int StartMinuteOfDay { get; set; }
        public int EndMinuteOfDay { get; set; }
        public int IntervalMinutes { get; set; } = 15;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Restaurant? Restaurant { get; set; }
        public RestaurantTable? RestaurantTable { get; set; }
    }
}
