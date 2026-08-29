using Core.Data.Enums;

namespace Core.Contracts.Account
{
    public class BillingProfileDto
    {
        public BillingCustomerType CustomerType { get; set; } = BillingCustomerType.Person;
        public string? ReceiptEmail { get; set; }
        public string? PersonName { get; set; }
        public string? CompanyName { get; set; }
        public string? TaxId { get; set; }
        public string? BillingAddressLine1 { get; set; }
        public string? BillingAddressLine2 { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingPostalCode { get; set; }
        public string? BillingCountry { get; set; }
        public string? DeliveryContactName { get; set; }
        public string? DeliveryPhone { get; set; }
        public string? DeliveryAddressLine1 { get; set; }
        public string? DeliveryAddressLine2 { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? DeliveryCountry { get; set; }
    }
}
