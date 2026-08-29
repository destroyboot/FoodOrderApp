namespace Core.Data.Entities
{
    public class IngredientTranslation
    {
        public int Id { get; set; }
        public int IngredientId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
