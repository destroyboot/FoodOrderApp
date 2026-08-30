namespace Core.Data.Entities
{
    public class CuisineTranslation
    {
        public int Id { get; set; }
        public int CuisineId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
