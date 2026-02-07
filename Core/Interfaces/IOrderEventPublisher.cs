using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IOrderEventPublisher
    {
        Task PublishNewOrderAsync(
            int orderId,
            OrderStatus status,
            OrderType orderType,
            string? tableNumber,
            decimal total,
            DateTime createdAt,
            CancellationToken ct = default);

        Task PublishOrderStatusChangedAsync(
            int orderId,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default);
    }
}
