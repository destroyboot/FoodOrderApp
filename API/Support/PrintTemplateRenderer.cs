using Core.Data.Entities;
using Core.Interfaces;
using Ganss.Xss;
using HandlebarsDotNet;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace API.Support;

public interface IPrintTemplateRenderer
{
    Task<IReadOnlyList<PrintTemplateDefinition>> GetDefinitionsAsync(CancellationToken ct);
    Task<string> RenderHtmlAsync(string code, object model, CancellationToken ct);
    Task<string> RenderHtmlAsync(string code, string? culture, string? fallbackCulture, object model, CancellationToken ct);
    string RenderDefaultHtml(string code, object model);
    string Sanitize(string html);
}

public sealed class PrintTemplateRenderer : IPrintTemplateRenderer, IEmailTemplateRenderer
{
    private readonly AppDbContext _db;
    private readonly HtmlSanitizer _sanitizer;

    public PrintTemplateRenderer(AppDbContext db)
    {
        _db = db;
        _sanitizer = CreateSanitizer();
    }

    public Task<IReadOnlyList<PrintTemplateDefinition>> GetDefinitionsAsync(CancellationToken ct)
        => Task.FromResult(PrintTemplateDefaults.All);

    public async Task<string> RenderHtmlAsync(string code, object model, CancellationToken ct)
        => await RenderHtmlAsync(code, "pl-PL", "pl-PL", model, ct);

    public async Task<string> RenderHtmlAsync(string code, string? culture, string? fallbackCulture, object model, CancellationToken ct)
    {
        var requestedCulture = NormalizeCulture(culture);
        var defaultCulture = NormalizeCulture(fallbackCulture);
        var template = await _db.PrintTemplates
            .AsNoTracking()
            .Where(x => x.Code == code && x.IsActive)
            .Where(x => x.Culture == requestedCulture || x.Culture == defaultCulture)
            .OrderBy(x => x.Culture == requestedCulture ? 0 : 1)
            .FirstOrDefaultAsync(ct);
        var html = template?.HtmlTemplate ?? PrintTemplateDefaults.GetRequired(code).HtmlTemplate;
        return Render(html, model);
    }

    public string RenderDefaultHtml(string code, object model)
        => Render(PrintTemplateDefaults.GetRequired(code).HtmlTemplate, model);

    public string Sanitize(string html)
        => _sanitizer.Sanitize(string.IsNullOrWhiteSpace(html) ? string.Empty : html);

    private string Render(string html, object model)
    {
        var sanitized = Sanitize(html);
        var template = Handlebars.Compile(sanitized);
        return template(model);
    }

    private static string NormalizeCulture(string? culture)
        => string.IsNullOrWhiteSpace(culture) ? "pl-PL" : culture.Trim();

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedSchemes.Add("data");
        sanitizer.AllowedAttributes.Add("style");
        sanitizer.AllowedAttributes.Add("align");
        sanitizer.AllowedAttributes.Add("valign");
        sanitizer.AllowedAttributes.Add("colspan");
        sanitizer.AllowedAttributes.Add("rowspan");
        sanitizer.AllowedAttributes.Add("width");
        sanitizer.AllowedAttributes.Add("height");

        foreach (var property in new[]
        {
            "background-color",
            "border",
            "border-bottom",
            "border-collapse",
            "border-color",
            "border-left",
            "border-right",
            "border-style",
            "border-top",
            "border-width",
            "color",
            "font",
            "font-family",
            "font-size",
            "font-style",
            "font-weight",
            "height",
            "line-height",
            "margin",
            "margin-bottom",
            "margin-left",
            "margin-right",
            "margin-top",
            "max-width",
            "min-width",
            "padding",
            "padding-bottom",
            "padding-left",
            "padding-right",
            "padding-top",
            "text-align",
            "text-decoration",
            "text-transform",
            "vertical-align",
            "white-space",
            "width"
        })
        {
            sanitizer.AllowedCssProperties.Add(property);
        }

        return sanitizer;
    }
}
