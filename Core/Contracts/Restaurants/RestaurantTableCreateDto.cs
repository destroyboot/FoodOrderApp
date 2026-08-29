namespace Core.Contracts.Restaurants
{
    public class RestaurantTableCreateDto
    {
        public string Label { get; set; } = default!;
        public int? Seats { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsReservable { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
