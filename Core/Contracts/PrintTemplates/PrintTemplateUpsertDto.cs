namespace Core.Contracts.PrintTemplates
{
    public class PrintTemplateUpsertDto
    {
        public string Culture { get; set; } = "pl-PL";
        public string Name { get; set; } = default!;
        public string Description { get; set; } = string.Empty;
        public string HtmlTemplate { get; set; } = default!;
        public bool IsActive { get; set; } = true;
    }
}
