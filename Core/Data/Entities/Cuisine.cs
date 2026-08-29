namespace Core.Data.Entities
{
    public class Cuisine
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
    }
}
