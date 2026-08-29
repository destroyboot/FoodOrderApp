using Core.Data.Enums;

namespace Core.Data.Entities
{
    public class OrderTypeOption
    {
        public int Id { get; set; }
        public OrderType Value { get; set; }
        public string Code { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public ICollection<OrderTypeOptionTranslation> Translations { get; set; } = new List<OrderTypeOptionTranslation>();
    }
}
