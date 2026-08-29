namespace Core.Contracts.Reservations
{
    public class ReservationTableAvailabilityDto
    {
        public int TableId { get; set; }
        public string Label { get; set; } = default!;
        public int? Seats { get; set; }
        public List<string> AvailableStartTimes { get; set; } = new();
        public List<ReservationBlockedRangeDto> BlockedRanges { get; set; } = new();
    }
}
