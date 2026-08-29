namespace Core.Data.Entities
{
    public class MenuItemIngredient
    {
        public int Id { get; set; }
        public int MenuItemId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public bool IsDefault { get; set; } = true;
        public bool IsRemovable { get; set; } = false;
        public bool IsSubstitute { get; set; } = false;

        public MenuItem? MenuItem { get; set; }
        public Ingredient? Ingredient { get; set; }
    }
}
