namespace Core.Contracts.Users
{
    public class AdminUserBulkRestaurantAssignmentDto
    {
        public List<string> UserIds { get; set; } = new();
        public int RestaurantId { get; set; }
        public string Role { get; set; } = default!;
    }
}
