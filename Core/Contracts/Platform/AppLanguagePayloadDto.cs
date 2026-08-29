namespace Core.Contracts.Platform
{
    public class AppLanguagePayloadDto
    {
        public string DefaultCulture { get; set; } = "pl-PL";
        public List<AppLanguageDto> Languages { get; set; } = new();
    }
}
