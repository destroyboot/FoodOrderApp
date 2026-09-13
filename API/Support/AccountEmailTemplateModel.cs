using System.Globalization;

namespace API.Support;

internal static class AccountEmailTemplateModel
{
    public static object Code(string email, string code, DateTime? expiresAtUtc)
        => new
        {
            account = new { email },
            code,
            expiresAt = expiresAtUtc.HasValue
                ? expiresAtUtc.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC"
                : string.Empty
        };

    public static object Link(string email, string link)
        => new
        {
            account = new { email },
            link
        };

    public static object Simple(string email)
        => new
        {
            account = new { email }
        };
}
