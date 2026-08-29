using Core.Data.Enums;

namespace Core.Data.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public int? RestaurantTableId { get; set; }
        public string? CustomerId { get; set; }
        public string GuestName { get; set; } = default!;
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public int PartySize { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Requested;
        public DateTime? CancelledAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Restaurant? Restaurant { get; set; }
        public RestaurantTable? RestaurantTable { get; set; }
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
