namespace Core.Contracts.Menu
{
    public class MenuItemCustomizationDto
    {
        public int MenuItemId { get; set; }
        public bool EnableIngredientSwap { get; set; }
        public decimal ExtraIngredientPrice { get; set; }
        public List<MenuItemIngredientOptionDto> DishIngredients { get; set; } = new();
        public List<int> RemovableIngredientIds { get; set; } = new();
        public List<MenuItemIngredientOptionDto> SubstituteIngredients { get; set; } = new();
    }
}
