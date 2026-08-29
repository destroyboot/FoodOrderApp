namespace Core.Contracts.Ingredients
{
    public class MenuItemCustomizationUpsertDto
    {
        public bool EnableIngredientSwap { get; set; }
        public List<int> DishIngredientIds { get; set; } = new();
        public List<int> RemovableIngredientIds { get; set; } = new();
        public List<int> SubstituteIngredientIds { get; set; } = new();
    }
}
