using System.Text.Json;

namespace Core.Contracts.AdminData
{
    public class AdminDataRowUpsertDto
    {
        public Dictionary<string, JsonElement?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
