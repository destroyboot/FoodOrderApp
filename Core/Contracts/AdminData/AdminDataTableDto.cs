namespace Core.Contracts.AdminData
{
    public class AdminDataTableDto
    {
        public string TableName { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string PrimaryKeyName { get; set; } = default!;
        public bool IsRestaurantScoped { get; set; }
        public AdminDataTablePermissionsDto Permissions { get; set; } = new();
        public List<AdminDataColumnDto> Columns { get; set; } = new();
    }
}
