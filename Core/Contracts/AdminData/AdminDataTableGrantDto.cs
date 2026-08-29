namespace Core.Contracts.AdminData
{
    public class AdminDataTableGrantDto
    {
        public string TableName { get; set; } = default!;
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
}
