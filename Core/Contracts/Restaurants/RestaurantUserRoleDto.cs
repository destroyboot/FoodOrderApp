namespace Core.Contracts.Restaurants
{
    public class RestaurantUserRoleDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string UserId { get; set; } = default!;
        public string? Email { get; set; }
        public string Role { get; set; } = default!;
        public bool IsPendingInvite { get; set; }
        public bool IsAwaitingAssignment { get; set; }
        public int? InviteId { get; set; }
    }
}
