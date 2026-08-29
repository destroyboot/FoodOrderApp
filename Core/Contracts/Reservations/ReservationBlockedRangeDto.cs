namespace Core.Contracts.Reservations
{
    public class ReservationBlockedRangeDto
    {
        public string StartTime { get; set; } = default!;
        public string EndTime { get; set; } = default!;
        public string Label { get; set; } = default!;
    }
}
