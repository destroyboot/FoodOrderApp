namespace Core.Contracts.Reservations
{
    public class ReservationCreateDto
    {
        public int RestaurantId { get; set; }
        public int? RestaurantTableId { get; set; }
        public int PartySize { get; set; } = 1;
        public DateTime StartAt { get; set; }
        public string? Note { get; set; }
    }
}
