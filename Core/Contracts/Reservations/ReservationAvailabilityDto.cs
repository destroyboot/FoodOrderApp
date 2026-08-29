namespace Core.Contracts.Reservations
{
    public class ReservationAvailabilityDto
    {
        public int RestaurantId { get; set; }
        public DateTime Date { get; set; }
        public int SlotMinutes { get; set; }
        public List<ReservationTableAvailabilityDto> Tables { get; set; } = new();
    }
}
