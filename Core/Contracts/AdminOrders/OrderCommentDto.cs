namespace Core.Contracts.AdminOrders
{
    public class OrderCommentDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? AuthorUserId { get; set; }
        public string? AuthorRole { get; set; }
        public string Body { get; set; } = default!;
        public bool IsCustomerVisible { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
