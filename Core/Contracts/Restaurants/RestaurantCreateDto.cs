namespace Core.Contracts.Restaurants
{
    public class RestaurantCreateDto
    {
        public string Name { get; set; } = default!;
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? PostalCode { get; set; }
        public string? HouseNumber { get; set; }
        public List<string> CuisineTypes { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
