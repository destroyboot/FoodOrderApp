namespace Core.Data.Entities
{
    public class PrintTemplate
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Culture { get; set; } = "pl-PL";
        public string Name { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public string DocumentKind { get; set; } = default!;
        public string HtmlTemplate { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? UpdatedByUserId { get; set; }
    }
}
