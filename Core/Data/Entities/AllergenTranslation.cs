namespace Core.Data.Entities
{
    public class AllergenTranslation
    {
        public int Id { get; set; }
        public int AllergenId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
