using Core.Data.Enums;
using Core.Interfaces;
using System.Text.Json;

namespace Core.Services
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly IOrderRepository _orders;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;
        private readonly IOrderEventPublisher _events;
        private readonly INotificationService _notifications;
        private readonly IOrderStatusEmailService _statusEmails;

        public AdminOrderService(
            IOrderRepository orders,
            IAsyncQueryExecutor q,
            IUnitOfWork uow,
            IOrderEventPublisher events,
            INotificationService notifications,
            IOrderStatusEmailService statusEmails)
        {
            _orders = orders;
            _q = q;
            _uow = uow;
            _events = events;
            _notifications = notifications;
            _statusEmails = statusEmails;
        }

        public async Task ChangeStatusAsync(
            int orderId,
            OrderStatus newStatus,
            string userId,
            IReadOnlyCollection<string> roles,
            CancellationToken ct = default)
        {
            var order = await _q.FirstOrDefaultAsync(
                _orders.Query(tracked: true).Where(o => o.Id == orderId),
                ct);

            if (order is null)
                throw new InvalidOperationException("Order not found.");

            if (order.Status == OrderStatus.Draft)
                throw new InvalidOperationException("Draft orders cannot be processed by staff.");

            var oldStatus = order.Status;

            if (!IsAllowedTransition(oldStatus, newStatus))
                throw new InvalidOperationException($"Invalid status transition: {oldStatus} -> {newStatus}");

            if (!IsRoleAllowedForTargetStatus(roles, newStatus))
                throw new InvalidOperationException("Your role is not allowed to set this status.");

            order.Status = newStatus;

            await _uow.SaveChangesAsync(ct);

            var oldStatusText = FormatStatusForCustomer(order.OrderType, oldStatus);
            var newStatusText = FormatStatusForCustomer(order.OrderType, order.Status);

            await _notifications.CreateAsync(
                ownerKey: order.CustomerId!,
                type: NotificationType.OrderStatusChanged,
                title: "Order status updated",
                body: $"Order #{order.Id}: {oldStatusText} -> {newStatusText}",
                payloadJson: JsonSerializer.Serialize(new
                {
                    type = "order-status-changed",
                    orderId = order.Id,
                    oldStatus = oldStatus.ToString(),
                    newStatus = order.Status.ToString(),
                    url = "/orders"
                }),
                ct: ct);

            await _statusEmails.TrySendStatusChangedEmailAsync(
                ownerKey: order.CustomerId!,
                orderId: order.Id,
                oldStatus: oldStatus,
                newStatus: newStatus,
                ct: ct);

            await _events.PublishOrderStatusChangedAsync(order.Id, oldStatus, newStatus, ct);
        }

        private static bool IsRoleAllowedForTargetStatus(IReadOnlyCollection<string> roles, OrderStatus target)
        {
            if (roles.Contains("Admin") || roles.Contains("RestaurantAdmin")) return true;

            if (roles.Contains("Waiter"))
                return target is OrderStatus.Accepted
                    or OrderStatus.SentToKitchen
                    or OrderStatus.Ready
                    or OrderStatus.Completed
                    or OrderStatus.Cancelled;

            if (roles.Contains("Chef"))
                return target is OrderStatus.Preparing or OrderStatus.ReadyForWaiter;

            if (roles.Contains("DeliveryDriver"))
                return target is OrderStatus.OutForDelivery or OrderStatus.Delivered;

            return false;
        }

        private static bool IsAllowedTransition(OrderStatus current, OrderStatus next)
        {
            if (current == next) return true;

            return current switch
            {
                OrderStatus.Pending => next is OrderStatus.Accepted or OrderStatus.Cancelled,
                OrderStatus.Accepted => next is OrderStatus.SentToKitchen or OrderStatus.Preparing or OrderStatus.Cancelled,
                OrderStatus.SentToKitchen => next is OrderStatus.Preparing or OrderStatus.Cancelled,
                OrderStatus.Preparing => next is OrderStatus.Ready or OrderStatus.ReadyForWaiter or OrderStatus.OutForDelivery or OrderStatus.Cancelled,
                OrderStatus.Ready => next is OrderStatus.ReadyForWaiter or OrderStatus.OutForDelivery or OrderStatus.Completed,
                OrderStatus.OutForDelivery => next is OrderStatus.Delivered or OrderStatus.Cancelled,
                OrderStatus.ReadyForWaiter => next is OrderStatus.Delivered or OrderStatus.Completed,
                OrderStatus.Delivered => next is OrderStatus.Completed,
                OrderStatus.Completed => false,
                OrderStatus.Cancelled => false,
                OrderStatus.Draft => false,
                _ => false
            };
        }

        private static string FormatStatusForCustomer(OrderType orderType, OrderStatus status)
        {
            if (orderType == OrderType.Takeaway && status == OrderStatus.Ready)
                return "Ready for pickup";

            if (orderType == OrderType.Delivery)
            {
                if (status == OrderStatus.Ready) return "Ready for dispatch";
                if (status == OrderStatus.OutForDelivery) return "Out for delivery";
            }

            return status.ToString();
        }
    }
}
