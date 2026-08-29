using Core.Contracts.Account;
using Core.Data.Enums;
using Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers
{
    [ApiController]
    [Route("api/account/billing-profile")]
    [Authorize]
    public class BillingProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _users;

        public BillingProfileController(UserManager<ApplicationUser> users)
        {
            _users = users;
        }

        [HttpGet]
        public async Task<ActionResult<BillingProfileDto>> Get(CancellationToken ct)
        {
            var user = await GetCurrentUserAsync(ct);
            return Ok(Map(user));
        }

        [HttpPut]
        public async Task<IActionResult> Put(BillingProfileDto dto, CancellationToken ct)
        {
            Validate(dto);

            var user = await GetCurrentUserAsync(ct);
            user.DefaultBillingCustomerType = (int)dto.CustomerType;
            user.DefaultBillingReceiptEmail = TrimOrNull(dto.ReceiptEmail);
            user.DefaultBillingPersonName = TrimOrNull(dto.PersonName);
            user.DefaultBillingCompanyName = TrimOrNull(dto.CompanyName);
            user.DefaultBillingTaxId = TrimOrNull(dto.TaxId);
            user.DefaultBillingAddressLine1 = TrimOrNull(dto.BillingAddressLine1);
            user.DefaultBillingAddressLine2 = TrimOrNull(dto.BillingAddressLine2);
            user.DefaultBillingCity = TrimOrNull(dto.BillingCity);
            user.DefaultBillingPostalCode = TrimOrNull(dto.BillingPostalCode);
            user.DefaultBillingCountry = TrimOrNull(dto.BillingCountry);
            user.DefaultDeliveryContactName = TrimOrNull(dto.DeliveryContactName);
            user.DefaultDeliveryPhone = TrimOrNull(dto.DeliveryPhone);
            user.DefaultDeliveryAddressLine1 = TrimOrNull(dto.DeliveryAddressLine1);
            user.DefaultDeliveryAddressLine2 = TrimOrNull(dto.DeliveryAddressLine2);
            user.DefaultDeliveryCity = TrimOrNull(dto.DeliveryCity);
            user.DefaultDeliveryPostalCode = TrimOrNull(dto.DeliveryPostalCode);
            user.DefaultDeliveryCountry = TrimOrNull(dto.DeliveryCountry);

            await _users.UpdateAsync(user);
            return NoContent();
        }

        private async Task<ApplicationUser> GetCurrentUserAsync(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException("Missing user id.");
            var user = await _users.FindByIdAsync(userId)
                ?? throw new InvalidOperationException("User not found.");
            return user;
        }

        private static BillingProfileDto Map(ApplicationUser user) => new()
        {
            CustomerType = (BillingCustomerType)user.DefaultBillingCustomerType,
            ReceiptEmail = user.DefaultBillingReceiptEmail,
            PersonName = user.DefaultBillingPersonName,
            CompanyName = user.DefaultBillingCompanyName,
            TaxId = user.DefaultBillingTaxId,
            BillingAddressLine1 = user.DefaultBillingAddressLine1,
            BillingAddressLine2 = user.DefaultBillingAddressLine2,
            BillingCity = user.DefaultBillingCity,
            BillingPostalCode = user.DefaultBillingPostalCode,
            BillingCountry = user.DefaultBillingCountry,
            DeliveryContactName = user.DefaultDeliveryContactName,
            DeliveryPhone = user.DefaultDeliveryPhone,
            DeliveryAddressLine1 = user.DefaultDeliveryAddressLine1,
            DeliveryAddressLine2 = user.DefaultDeliveryAddressLine2,
            DeliveryCity = user.DefaultDeliveryCity,
            DeliveryPostalCode = user.DefaultDeliveryPostalCode,
            DeliveryCountry = user.DefaultDeliveryCountry
        };

        private static void Validate(BillingProfileDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.ReceiptEmail) && !new EmailAddressAttribute().IsValid(dto.ReceiptEmail))
                throw new InvalidOperationException("Receipt email is invalid.");

            var hasBillingData =
                !string.IsNullOrWhiteSpace(dto.ReceiptEmail)
                || !string.IsNullOrWhiteSpace(dto.PersonName)
                || !string.IsNullOrWhiteSpace(dto.CompanyName)
                || !string.IsNullOrWhiteSpace(dto.TaxId)
                || !string.IsNullOrWhiteSpace(dto.BillingAddressLine1)
                || !string.IsNullOrWhiteSpace(dto.BillingAddressLine2)
                || !string.IsNullOrWhiteSpace(dto.BillingCity)
                || !string.IsNullOrWhiteSpace(dto.BillingPostalCode)
                || !string.IsNullOrWhiteSpace(dto.BillingCountry);

            if (hasBillingData)
            {
                if (dto.CustomerType == BillingCustomerType.Company)
                {
                    if (string.IsNullOrWhiteSpace(dto.CompanyName))
                        throw new InvalidOperationException("Company name is required for invoice profile.");
                    if (string.IsNullOrWhiteSpace(dto.TaxId))
                        throw new InvalidOperationException("Tax ID is required for invoice profile.");
                    if (!dto.TaxId.All(char.IsDigit))
                        throw new InvalidOperationException("Tax ID must contain only digits.");
                    if (string.IsNullOrWhiteSpace(dto.BillingAddressLine1)
                        || string.IsNullOrWhiteSpace(dto.BillingCity)
                        || string.IsNullOrWhiteSpace(dto.BillingPostalCode))
                        throw new InvalidOperationException("Billing address is required for invoice profile.");
                }
                else if (string.IsNullOrWhiteSpace(dto.PersonName))
                {
                    throw new InvalidOperationException("Name is required for invoice profile.");
                }
            }

            if (!string.IsNullOrWhiteSpace(dto.DeliveryPhone) && !dto.DeliveryPhone.All(ch => char.IsDigit(ch) || "+-() ".Contains(ch)))
                throw new InvalidOperationException("Delivery phone is invalid.");
        }

        private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
