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
        sanitizer.AllowedCssProperties.Add("font-size");
        sanitizer.AllowedCssProperties.Add("font-family");
        sanitizer.AllowedCssProperties.Add("font-weight");
        sanitizer.AllowedCssProperties.Add("font-style");
        sanitizer.AllowedCssProperties.Add("text-align");
        sanitizer.AllowedCssProperties.Add("text-decoration");
        sanitizer.AllowedCssProperties.Add("color");
        sanitizer.AllowedCssProperties.Add("background-color");
        return sanitizer;
    }
}
