namespace Core.Contracts.Platform
{
    public class AppTextDictionaryDto
    {
        public string Culture { get; set; } = "pl-PL";
        public string DefaultCulture { get; set; } = "pl-PL";
        public Dictionary<string, string> Texts { get; set; } = new();
    }
}
