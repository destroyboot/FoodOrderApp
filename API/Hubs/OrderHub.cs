using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace API.Hubs
{
    [Authorize(Roles = "Admin,RestaurantAdmin,Waiter,Chef,DeliveryDriver")]
    public class OrderHub : Hub
    {
    }
}
