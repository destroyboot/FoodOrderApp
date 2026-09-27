namespace Core.Data.Entities
{
    public class AdminFeaturePermission
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = default!;
        public string FeatureKey { get; set; } = default!;
        public bool IsAllowed { get; set; }
    }
}
