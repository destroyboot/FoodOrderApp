namespace Core.Contracts.AdminOrders
{
    public class OrderCommentCreateDto
    {
        public string Body { get; set; } = default!;
        public bool IsCustomerVisible { get; set; }
    }
}
