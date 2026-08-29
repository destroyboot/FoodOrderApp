namespace Core.Contracts.Reservations
{
    public class ReservationScheduleDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int RestaurantTableId { get; set; }
        public string? TableLabel { get; set; }
        public int? Seats { get; set; }
        public int StartMinuteOfDay { get; set; }
        public int EndMinuteOfDay { get; set; }
        public int IntervalMinutes { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
