namespace Core.Contracts.Ingredients
{
    public class MenuItemIngredientDto
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public decimal Quantity { get; set; }
    }
}
