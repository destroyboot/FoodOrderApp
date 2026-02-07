using API.Hubs;
using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace API.Events
{
    public class SignalROrderEventPublisher : IOrderEventPublisher
    {
        private readonly IHubContext<OrderHub> _hub;

        public SignalROrderEventPublisher(IHubContext<OrderHub> hub)
        {
            _hub = hub;
        }

        public Task PublishNewOrderAsync(
            int orderId,
            OrderStatus status,
            OrderType orderType,
            string? tableNumber,
            decimal total,
            DateTime createdAt,
            CancellationToken ct = default)
        {
            return _hub.Clients.All.SendAsync("NewOrder", new
            {
                orderId,
                status = status.ToString(),
                orderType = orderType.ToString(),
                tableNumber,
                total,
                createdAt
            }, ct);
        }

        public Task PublishOrderStatusChangedAsync(
            int orderId,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default)
        {
            return _hub.Clients.All.SendAsync("OrderStatusChanged", new
            {
                orderId,
                oldStatus = oldStatus.ToString(),
                newStatus = newStatus.ToString()
            }, ct);
        }
    }
}
