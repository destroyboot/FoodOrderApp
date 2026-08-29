using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Auth
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAtUtc { get; set; }
        public int SuccessfulLoginCount { get; set; } = 0;
        public string? RegistrationCodeHash { get; set; }
        public DateTime? RegistrationCodeExpiresAt { get; set; }
        public DateTime? RegistrationResendAvailableAt { get; set; }
        public int RegistrationResendCount { get; set; } = 0;
        public bool WantsOrderStatusEmails { get; set; } = false;
        public int DefaultBillingCustomerType { get; set; } = 0;
        public string? DefaultBillingReceiptEmail { get; set; }
        public string? DefaultBillingPersonName { get; set; }
        public string? DefaultBillingCompanyName { get; set; }
        public string? DefaultBillingTaxId { get; set; }
        public string? DefaultBillingAddressLine1 { get; set; }
        public string? DefaultBillingAddressLine2 { get; set; }
        public string? DefaultBillingCity { get; set; }
        public string? DefaultBillingPostalCode { get; set; }
        public string? DefaultBillingCountry { get; set; }
        public string? DefaultDeliveryContactName { get; set; }
        public string? DefaultDeliveryPhone { get; set; }
        public string? DefaultDeliveryAddressLine1 { get; set; }
        public string? DefaultDeliveryAddressLine2 { get; set; }
        public string? DefaultDeliveryCity { get; set; }
        public string? DefaultDeliveryPostalCode { get; set; }
        public string? DefaultDeliveryCountry { get; set; }
        public string? PreferredCulture { get; set; }
        public string? PendingEmail { get; set; }
        public string? EmailChangeCodeHash { get; set; }
        public DateTime? EmailChangeCodeExpiresAt { get; set; }
        public DateTime? EmailChangeResendAvailableAt { get; set; }
        public int EmailChangeResendCount { get; set; } = 0;
        public string? AccountDeletionCodeHash { get; set; }
        public DateTime? AccountDeletionCodeExpiresAt { get; set; }
    }
}
