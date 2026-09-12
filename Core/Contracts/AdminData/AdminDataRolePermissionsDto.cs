namespace Core.Contracts.AdminData
{
    public class AdminDataRolePermissionsDto
    {
        public string RoleName { get; set; } = default!;
        public List<AdminDataTableGrantDto> Grants { get; set; } = new();
    }
}
