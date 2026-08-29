namespace Core.Contracts.Users
{
    public class AdminUserCreateDto
    {
        public string Email { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Password { get; set; } = default!;
        public bool EmailConfirmed { get; set; } = true;
        public bool IsAppAdmin { get; set; }
        public List<AdminUserRestaurantAssignmentDto> RestaurantAssignments { get; set; } = new();
    }
}
