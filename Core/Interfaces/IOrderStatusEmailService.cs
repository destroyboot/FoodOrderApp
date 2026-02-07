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
            string ownerKey,
            int orderId,
            OrderStatus oldStatus,
            OrderStatus newStatus,
            CancellationToken ct = default);
    }
}
