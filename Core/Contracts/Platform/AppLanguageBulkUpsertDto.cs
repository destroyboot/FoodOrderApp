namespace Core.Contracts.Platform
{
    public class AppLanguageBulkUpsertDto
    {
        public List<AppLanguageDto> Items { get; set; } = new();
    }
}
