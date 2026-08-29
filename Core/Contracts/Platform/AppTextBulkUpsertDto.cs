namespace Core.Contracts.Platform
{
    public class AppTextBulkUpsertDto
    {
        public List<AppTextTranslationDto> Items { get; set; } = new();
    }
}
