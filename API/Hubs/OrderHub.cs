using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    [Authorize(Roles = "Admin,Waiter,Chef")] // staff only
    public class OrderHub : Hub
    {
        // Later: groups per RestaurantId etc.
    }
}
