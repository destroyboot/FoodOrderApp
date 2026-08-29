namespace Core.Data.Entities
{
    public class RestaurantTable
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public string Label { get; set; } = default!;
        public int? Seats { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsReservable { get; set; } = true;
        public int SortOrder { get; set; }

        public Restaurant? Restaurant { get; set; }
    }
}
