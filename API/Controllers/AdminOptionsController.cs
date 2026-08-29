using Core.Contracts.AdminOptions;
using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/admin/options")]
    [Authorize(Roles = "Admin")]
    public class AdminOptionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminOptionsController(AppDbContext db) => _db = db;

        [HttpGet("order-types")]
        public async Task<ActionResult<IReadOnlyList<OrderTypeOptionDto>>> GetOrderTypes(CancellationToken ct)
            => Ok(await _db.OrderTypeOptions
                .AsNoTracking()
                .Include(x => x.Translations)
                .OrderBy(x => x.SortOrder)
                .Select(x => new OrderTypeOptionDto
                {
                    Id = x.Id,
                    Value = x.Value,
                    Code = x.Code,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder,
                    Translations = x.Translations.Select(t => new OptionTranslationDto
                    {
                        Culture = t.Culture,
                        Name = t.Name
                    }).ToList()
                })
                .ToListAsync(ct));

        [HttpPost("order-types")]
        public async Task<IActionResult> CreateOrderType(OrderTypeOptionUpsertDto dto, CancellationToken ct)
        {
            ValidateOption(dto.Code, dto.Translations);
            var option = new OrderTypeOption
            {
                Value = dto.Value,
                Code = dto.Code.Trim(),
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                Translations = dto.Translations.Select(t => new OrderTypeOptionTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
                }).ToList()
            };
            _db.OrderTypeOptions.Add(option);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetOrderTypes), new { id = option.Id }, new { option.Id });
        }

        [HttpPut("order-types/{id:int}")]
        public async Task<IActionResult> UpdateOrderType(int id, OrderTypeOptionUpsertDto dto, CancellationToken ct)
        {
            ValidateOption(dto.Code, dto.Translations);
            var option = await _db.OrderTypeOptions
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (option is null) return NotFound();

            option.Value = dto.Value;
            option.Code = dto.Code.Trim();
            option.IsActive = dto.IsActive;
            option.SortOrder = dto.SortOrder;
            option.Translations.Clear();
            foreach (var translation in dto.Translations)
            {
                option.Translations.Add(new OrderTypeOptionTranslation
                {
                    Culture = translation.Culture.Trim(),
                    Name = translation.Name.Trim()
                });
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("payment-methods")]
        public async Task<ActionResult<IReadOnlyList<PaymentMethodOptionDto>>> GetPaymentMethods(CancellationToken ct)
            => Ok(await _db.PaymentMethodOptions
                .AsNoTracking()
                .Include(x => x.Translations)
                .OrderBy(x => x.SortOrder)
                .Select(x => new PaymentMethodOptionDto
                {
                    Id = x.Id,
                    Value = x.Value,
                    Code = x.Code,
                    IsActive = x.IsActive,
                    SortOrder = x.SortOrder,
                    Translations = x.Translations.Select(t => new OptionTranslationDto
                    {
                        Culture = t.Culture,
                        Name = t.Name
                    }).ToList()
                })
                .ToListAsync(ct));

        [HttpPost("payment-methods")]
        public async Task<IActionResult> CreatePaymentMethod(PaymentMethodOptionUpsertDto dto, CancellationToken ct)
        {
            ValidateOption(dto.Code, dto.Translations);
            var option = new PaymentMethodOption
            {
                Value = dto.Value,
                Code = dto.Code.Trim(),
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder,
                Translations = dto.Translations.Select(t => new PaymentMethodOptionTranslation
                {
                    Culture = t.Culture.Trim(),
                    Name = t.Name.Trim()
                }).ToList()
            };
            _db.PaymentMethodOptions.Add(option);
            await _db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetPaymentMethods), new { id = option.Id }, new { option.Id });
        }

        [HttpPut("payment-methods/{id:int}")]
        public async Task<IActionResult> UpdatePaymentMethod(int id, PaymentMethodOptionUpsertDto dto, CancellationToken ct)
        {
            ValidateOption(dto.Code, dto.Translations);
            var option = await _db.PaymentMethodOptions
                .Include(x => x.Translations)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (option is null) return NotFound();

            option.Value = dto.Value;
            option.Code = dto.Code.Trim();
            option.IsActive = dto.IsActive;
            option.SortOrder = dto.SortOrder;
            option.Translations.Clear();
            foreach (var translation in dto.Translations)
            {
                option.Translations.Add(new PaymentMethodOptionTranslation
                {
                    Culture = translation.Culture.Trim(),
                    Name = translation.Name.Trim()
                });
            }

            await _db.SaveChangesAsync(ct);
            return NoContent();
        }

        private static void ValidateOption(string code, IReadOnlyCollection<OptionTranslationDto> translations)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("Code is required.");

            if (translations.Count == 0 || translations.Any(t => string.IsNullOrWhiteSpace(t.Culture) || string.IsNullOrWhiteSpace(t.Name)))
                throw new InvalidOperationException("At least one valid translation is required.");
        }
    }
}
