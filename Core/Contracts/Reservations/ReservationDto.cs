using Core.Data.Enums;

namespace Core.Contracts.Reservations
{
    public class ReservationDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string? RestaurantName { get; set; }
        public int? RestaurantTableId { get; set; }
        public string? TableLabel { get; set; }
        public string? CustomerId { get; set; }
        public string GuestName { get; set; } = default!;
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public int PartySize { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public ReservationStatus Status { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
