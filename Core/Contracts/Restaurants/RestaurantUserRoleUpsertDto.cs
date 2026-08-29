namespace Core.Contracts.Restaurants
{
    public class RestaurantUserRoleUpsertDto
    {
        public string UserId { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
