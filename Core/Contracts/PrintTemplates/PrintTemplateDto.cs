namespace Core.Contracts.PrintTemplates
{
    public class PrintTemplateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Culture { get; set; } = "pl-PL";
        public string Name { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public string DocumentKind { get; set; } = default!;
        public string HtmlTemplate { get; set; } = default!;
        public bool IsActive { get; set; }
        public bool IsCustomized { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string? UpdatedByUserId { get; set; }
        public List<string> AvailableTokens { get; set; } = new();
    }
}
