namespace Core.Contracts.AdminData
{
    public class AdminFeatureGrantDto
    {
        public string FeatureKey { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string GroupName { get; set; } = default!;
        public bool IsAllowed { get; set; }
    }
}
