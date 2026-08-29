namespace Core.Contracts.AdminData
{
    public class AdminDataColumnDto
    {
        public string Name { get; set; } = default!;
        public string DataType { get; set; } = default!;
        public bool IsNullable { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsEditable { get; set; }
        public List<string> EnumValues { get; set; } = new();
        public string? ForeignKeyTableName { get; set; }
        public string? ForeignKeyPrimaryKeyName { get; set; }
        public string? ForeignKeyLabelPropertyName { get; set; }
    }
}
