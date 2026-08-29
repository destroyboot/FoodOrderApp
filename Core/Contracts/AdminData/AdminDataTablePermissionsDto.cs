namespace Core.Contracts.AdminData
{
    public class AdminDataTablePermissionsDto
    {
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
}
