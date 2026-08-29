namespace Core.Contracts.Menu
{
    public class MenuItemIngredientOptionDto
    {
        public int IngredientId { get; set; }
        public string Name { get; set; } = default!;
        public decimal Quantity { get; set; }
    }
}
