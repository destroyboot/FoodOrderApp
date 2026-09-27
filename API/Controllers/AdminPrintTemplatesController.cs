using API.Authorization;
using API.Support;
using Core.Contracts.PrintTemplates;
using Core.Data.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace API.Controllers;

[Authorize(Roles = "Admin,RestaurantAdmin,Waiter,Chef,DeliveryDriver")]
[AppFeatureAuthorize(AppFeatures.PrintTemplatesManage)]
[ApiController]
[Route("api/admin/print-templates")]
public class AdminPrintTemplatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPrintTemplateRenderer _renderer;

    public AdminPrintTemplatesController(AppDbContext db, IPrintTemplateRenderer renderer)
    {
        _db = db;
        _renderer = renderer;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrintTemplateDto>>> Get([FromQuery] string? culture, CancellationToken ct)
    {
        var requestedCulture = NormalizeCulture(culture);
        var templates = await TryLoadSavedTemplatesAsync(ct);
        var saved = templates
            .Where(x => string.Equals(x.Culture, requestedCulture, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var result = PrintTemplateDefaults.All
            .Select(definition =>
            {
                saved.TryGetValue(definition.Code, out var template);
                return ToDto(definition, template, requestedCulture);
            })
            .OrderBy(x => x.DocumentKind)
            .ThenBy(x => x.Name)
            .ToList();

        return Ok(result);
    }

    private async Task<List<PrintTemplate>> TryLoadSavedTemplatesAsync(CancellationToken ct)
    {
        try
        {
            return await _db.PrintTemplates
                .AsNoTracking()
                .ToListAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is 207 or 208)
        {
            return new List<PrintTemplate>();
        }
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<PrintTemplateDto>> GetOne(string code, [FromQuery] string? culture, CancellationToken ct)
    {
        var requestedCulture = NormalizeCulture(culture);
        var definition = PrintTemplateDefaults.GetRequired(code);
        var template = await _db.PrintTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == definition.Code && x.Culture == requestedCulture, ct);

        return Ok(ToDto(definition, template, requestedCulture));
    }

    [HttpPut("{code}")]
    public async Task<IActionResult> Save(string code, [FromBody] PrintTemplateUpsertDto dto, CancellationToken ct)
    {
        var requestedCulture = NormalizeCulture(dto.Culture);
        var definition = PrintTemplateDefaults.GetRequired(code);
        var template = await _db.PrintTemplates.FirstOrDefaultAsync(x => x.Code == definition.Code && x.Culture == requestedCulture, ct);
        var now = DateTime.UtcNow;

        if (template is null)
        {
            template = new PrintTemplate
            {
                Code = definition.Code,
                Culture = requestedCulture,
                DocumentKind = definition.DocumentKind,
                CreatedAtUtc = now
            };
            _db.PrintTemplates.Add(template);
        }

        template.Name = string.IsNullOrWhiteSpace(dto.Name) ? definition.Name : dto.Name.Trim();
        template.Description = dto.Description?.Trim() ?? string.Empty;
        template.HtmlTemplate = _renderer.Sanitize(dto.HtmlTemplate);
        template.IsActive = dto.IsActive;
        template.UpdatedAtUtc = now;
        template.UpdatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{code}/reset")]
    public async Task<IActionResult> Reset(string code, CancellationToken ct)
    {
        var requestedCulture = NormalizeCulture(Request.Query["culture"].ToString());
        var definition = PrintTemplateDefaults.GetRequired(code);
        var existing = await _db.PrintTemplates.FirstOrDefaultAsync(x => x.Code == definition.Code && x.Culture == requestedCulture, ct);
        if (existing is not null)
        {
            _db.PrintTemplates.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }

        return NoContent();
    }

    [HttpPost("{code}/preview")]
    public ActionResult<PrintTemplatePreviewDto> Preview(string code, [FromBody] PrintTemplateUpsertDto? dto)
    {
        PrintTemplateDefaults.GetRequired(code);
        var html = string.IsNullOrWhiteSpace(dto?.HtmlTemplate)
            ? _renderer.RenderDefaultHtml(code, BuildPreviewModel(code))
            : RenderSubmittedTemplate(dto.HtmlTemplate, code);

        return Ok(new PrintTemplatePreviewDto { Html = html });
    }

    private string RenderSubmittedTemplate(string html, string code)
    {
        var sanitized = _renderer.Sanitize(html);
        return HandlebarsDotNet.Handlebars.Compile(sanitized)(BuildPreviewModel(code));
    }

    private static PrintTemplateDto ToDto(PrintTemplateDefinition definition, PrintTemplate? template, string culture)
        => new()
        {
            Id = template?.Id ?? 0,
            Code = definition.Code,
            Culture = culture,
            Name = template?.Name ?? definition.Name,
            Description = template?.Description ?? definition.Description,
            DocumentKind = definition.DocumentKind,
            HtmlTemplate = template?.HtmlTemplate ?? definition.HtmlTemplate,
            IsActive = template?.IsActive ?? true,
            IsCustomized = template is not null,
            UpdatedAtUtc = template?.UpdatedAtUtc ?? DateTime.MinValue,
            UpdatedByUserId = template?.UpdatedByUserId,
            AvailableTokens = definition.AvailableTokens.ToList()
        };

    private static string NormalizeCulture(string? culture)
        => string.IsNullOrWhiteSpace(culture) ? "pl-PL" : culture.Trim();

    private static object BuildPreviewModel(string code)
    {
        var now = DateTime.UtcNow;
        var itemsTable = """
            <table style="width:100%;border-collapse:collapse" border="1" cellpadding="6">
              <thead><tr><th align="left">Pozycja</th><th>Ilość</th><th>Cena</th><th>Razem</th></tr></thead>
              <tbody>
                <tr><td>Edamame</td><td align="center">1</td><td align="right">16,00</td><td align="right">16,00</td></tr>
              </tbody>
            </table>
            """;

        var reportTable = """
            <table style="width:100%;border-collapse:collapse" border="1" cellpadding="6">
              <thead><tr><th align="left">Restaurant</th><th align="right">Orders</th><th align="right">Revenue</th></tr></thead>
              <tbody>
                <tr><td>Pierogi House</td><td align="right">42</td><td align="right">3250.00</td></tr>
                <tr><td>Sushi Spot</td><td align="right">31</td><td align="right">4180.00</td></tr>
              </tbody>
            </table>
            """;

        return new
        {
            report = new
            {
                title = PrintTemplateDefaults.GetRequired(code).Name,
                generatedAt = now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                dateFrom = now.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                dateTo = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                summaryHtml = "<ul><li>Total revenue: 7430.00</li><li>Orders: 73</li></ul>",
                tableHtml = reportTable
            },
            order = new
            {
                id = 123,
                number = "0000",
                createdAt = "2026-09-13 10:09:54Z",
                status = "Oczekujące",
                type = "Do stolika",
                paymentMethod = "Card",
                paymentStatus = "Paid",
                subtotal = "16,00",
                deliveryFee = "0,00",
                total = "16,00"
            },
            restaurant = new { name = "Sushi Garden" },
            customer = new { name = "Jan Kowalski", email = "jan.kowalski@example.com" },
            invoice = new { number = "INV-20260913-1070" },
            billing = new { name = "Jan Kowalski", taxId = "1234567890", address = "Main Street 10, Warsaw" },
            account = new { email = "jan.kowalski@example.com" },
            code = "123456",
            expiresAt = now.AddMinutes(20).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC",
            link = "http://localhost:5173/reset-password?email=jan.kowalski%40example.com&token=sample-token",
            oldStatus = "Accepted",
            newStatus = "Preparing",
            itemsTableHtml = itemsTable
        };
    }
}
