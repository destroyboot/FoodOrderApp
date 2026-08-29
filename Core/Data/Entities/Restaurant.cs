namespace Core.Data.Entities
{
    public class Restaurant
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? HouseNumber { get; set; }
        public string? CuisineType { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public RestaurantSettings? Settings { get; set; }
        public ICollection<RestaurantTable> Tables { get; set; } = new List<RestaurantTable>();
        public ICollection<RestaurantUserRole> UserRoles { get; set; } = new List<RestaurantUserRole>();
        public ICollection<RestaurantStaffInvite> StaffInvites { get; set; } = new List<RestaurantStaffInvite>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
