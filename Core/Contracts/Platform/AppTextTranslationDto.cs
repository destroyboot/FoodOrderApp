namespace Core.Contracts.Platform
{
    public class AppTextTranslationDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = default!;
        public string Culture { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string? Description { get; set; }
        public string? GroupName { get; set; }
        public bool IsActive { get; set; }
    }
}
