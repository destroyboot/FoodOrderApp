using Core.Contracts.Users;

namespace Core.Contracts.AdminData
{
    public class AdminDataUserGrantDto
    {
        public string UserId { get; set; } = default!;
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public List<AdminUserRestaurantAssignmentDto> RestaurantAssignments { get; set; } = new();
        public List<AdminDataTableGrantDto> Grants { get; set; } = new();
    }
}
