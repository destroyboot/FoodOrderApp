namespace Core.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        Task<string> RenderHtmlAsync(
            string code,
            string? culture,
            string? fallbackCulture,
            object model,
            CancellationToken ct = default);
    }
}
