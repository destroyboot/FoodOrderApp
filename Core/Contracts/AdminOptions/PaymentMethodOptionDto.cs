using Core.Data.Enums;

namespace Core.Contracts.AdminOptions
{
    public class PaymentMethodOptionDto
    {
        public int Id { get; set; }
        public PaymentMethod Value { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public List<OptionTranslationDto> Translations { get; set; } = new();
    }
}
