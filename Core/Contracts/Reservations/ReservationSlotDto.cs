namespace Core.Contracts.Reservations
{
    public class ReservationSlotDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int RestaurantTableId { get; set; }
        public string? TableLabel { get; set; }
        public int? Seats { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
