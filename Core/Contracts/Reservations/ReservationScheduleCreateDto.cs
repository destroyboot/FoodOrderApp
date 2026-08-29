namespace Core.Contracts.Reservations
{
    public class ReservationScheduleCreateDto
    {
        public int RestaurantId { get; set; }
        public List<int> TableIds { get; set; } = new();
        public string StartTime { get; set; } = "17:00";
        public string EndTime { get; set; } = "23:00";
        public int IntervalMinutes { get; set; } = 15;
    }
}
