namespace Core.Data.Entities
{
    public class RestaurantStaffInvite
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Email { get; set; } = default!;
        public string RequestedRole { get; set; } = default!;
        public string InviteToken { get; set; } = default!;
        public string? UserId { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string InvitedByUserId { get; set; } = default!;

        public Restaurant? Restaurant { get; set; }
    }
}
