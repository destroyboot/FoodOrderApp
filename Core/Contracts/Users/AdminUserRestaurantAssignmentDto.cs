namespace Core.Contracts.Users
{
    public class AdminUserRestaurantAssignmentDto
    {
        public int RestaurantId { get; set; }
        public string Role { get; set; } = default!;
        public string? RestaurantName { get; set; }
    }
}
