namespace Core.Utilities
{
    public static class TokenizedSearch
    {
        public static IReadOnlyList<string> SplitTerms(string? input)
        {
            return (input ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
