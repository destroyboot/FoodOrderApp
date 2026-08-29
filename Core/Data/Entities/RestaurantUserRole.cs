namespace Core.Data.Entities
{
    public class RestaurantUserRole
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string UserId { get; set; } = default!;
        public string Role { get; set; } = default!;

        public Restaurant? Restaurant { get; set; }
    }
}
