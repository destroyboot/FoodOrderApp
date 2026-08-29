namespace Core.Data.Entities
{
    public class AppLanguage
    {
        public int Id { get; set; }
        public string Culture { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string NativeName { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
    }
}
