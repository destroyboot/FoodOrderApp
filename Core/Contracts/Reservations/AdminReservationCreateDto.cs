namespace Core.Contracts.Reservations
{
    public class AdminReservationCreateDto
    {
        public int RestaurantId { get; set; }
        public int RestaurantTableId { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public int PartySize { get; set; } = 1;
        public DateTime StartAt { get; set; }
        public string? Note { get; set; }
    }
}
