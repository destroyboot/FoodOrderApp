namespace Core.Contracts.Reservations
{
    public class ReservationSlotGenerateDto
    {
        public int RestaurantId { get; set; }
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = "17:00";
        public string EndTime { get; set; } = "23:00";
        public int IntervalMinutes { get; set; } = 15;
        public List<int> TableIds { get; set; } = new();
    }
}
