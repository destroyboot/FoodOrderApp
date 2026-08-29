using Core.Data.Enums;

namespace Core.Contracts.AdminOptions
{
    public class OrderTypeOptionUpsertDto
    {
        public OrderType Value { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public List<OptionTranslationDto> Translations { get; set; } = new();
    }
}
