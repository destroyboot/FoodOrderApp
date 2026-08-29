using Core.Data.Enums;

namespace Core.Data.Entities
{
    public class Ingredient
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public IngredientUnit Unit { get; set; } = IngredientUnit.Gram;
        public decimal CostPerUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public string? AllergenCode { get; set; }

        public Restaurant? Restaurant { get; set; }
        public ICollection<IngredientTranslation> Translations { get; set; } = new List<IngredientTranslation>();
    }
}
