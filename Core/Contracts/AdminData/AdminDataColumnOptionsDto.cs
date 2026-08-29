namespace Core.Contracts.AdminData
{
    public class AdminDataColumnOptionsDto
    {
        public string ColumnName { get; set; } = default!;
        public List<AdminDataOptionDto> Options { get; set; } = new();
    }
}
