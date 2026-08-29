namespace Core.Data.Entities
{
    public class PaymentMethodOptionTranslation
    {
        public int Id { get; set; }
        public int PaymentMethodOptionId { get; set; }
        public string Culture { get; set; } = default!;
        public string Name { get; set; } = default!;
    }
}
