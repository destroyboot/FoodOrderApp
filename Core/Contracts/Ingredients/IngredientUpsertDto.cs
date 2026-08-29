using Core.Data.Enums;

namespace Core.Contracts.Ingredients
{
    public class IngredientUpsertDto
    {
        public int RestaurantId { get; set; }
        public IngredientUnit Unit { get; set; } = IngredientUnit.Gram;
        public decimal CostPerUnit { get; set; }
        public bool IsActive { get; set; } = true;
        public string? AllergenCode { get; set; }
        public List<string> AllergenCodes { get; set; } = new();
        public List<IngredientTranslationDto> Translations { get; set; } = new();
    }
}
