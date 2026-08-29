using Core.Data.Enums;

namespace Core.Contracts.Ingredients
{
    public class IngredientDto
    {
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public IngredientUnit Unit { get; set; }
        public decimal CostPerUnit { get; set; }
        public bool IsActive { get; set; }
        public string? AllergenCode { get; set; }
        public List<string> AllergenCodes { get; set; } = new();
        public string? Name { get; set; }
        public List<IngredientTranslationDto> Translations { get; set; } = new();
    }
}
