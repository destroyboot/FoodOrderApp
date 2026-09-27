namespace Core.Contracts.AdminData
{
    public class AdminFeatureRolePermissionsDto
    {
        public string RoleName { get; set; } = default!;
        public List<AdminFeatureGrantDto> Grants { get; set; } = new();
    }
}
