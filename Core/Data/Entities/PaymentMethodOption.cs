using Core.Data.Enums;

namespace Core.Data.Entities
{
    public class PaymentMethodOption
    {
        public int Id { get; set; }
        public PaymentMethod Value { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public ICollection<PaymentMethodOptionTranslation> Translations { get; set; } = new List<PaymentMethodOptionTranslation>();
    }
}
