namespace Core.Contracts.AdminOrders
{
    public class DeliveryDriverOptionDto
    {
        public string UserId { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Email { get; set; }
    }
}
