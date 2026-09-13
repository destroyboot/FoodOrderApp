using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IOrderStatusEmailService
    {
        Task TrySendStatusChangedEmailAsync(
            string? ownerKey,
            string? fallbackEmail,
            int orderId,
            string displayOrderNumber,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default);
    }
}
