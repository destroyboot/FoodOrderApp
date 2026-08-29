namespace Core.Contracts.Restaurants
{
    public class RestaurantTableDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Label { get; set; } = default!;
        public int? Seats { get; set; }
        public bool IsActive { get; set; }
        public bool IsReservable { get; set; }
        public int SortOrder { get; set; }
    }
}
