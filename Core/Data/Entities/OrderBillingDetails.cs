using Core.Data.Enums;

namespace Core.Data.Entities
{
    public class OrderBillingDetails
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public BillingCustomerType CustomerType { get; set; } = BillingCustomerType.Person;
        public InvoiceStatus InvoiceStatus { get; set; } = InvoiceStatus.NotRequested;

        public string? ReceiptEmail { get; set; }
        public string? PersonName { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxId { get; set; }
        public string? BillingAddressLine1 { get; set; }
        public string? BillingAddressLine2 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        public DateTime? InvoiceIssuedAt { get; set; }
        public DateTime? InvoiceSentAt { get; set; }

        public Order? Order { get; set; }
    }
}
