namespace Core.Data.Entities
{
    public class AdminTablePermission
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = default!;
        public string TableName { get; set; } = default!;
        public bool CanRead { get; set; }
        public bool CanCreate { get; set; }
        public bool CanUpdate { get; set; }
        public bool CanDelete { get; set; }
    }
}
