namespace Core.Data.Entities
{
    public class OrderTypeOptionTranslation
    {
        public int Id { get; set; }
        public int OrderTypeOptionId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
