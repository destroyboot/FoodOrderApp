using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IAdminOrderService
    {
        Task ChangeStatusAsync(int orderId, OrderStatus newStatus, string userId, IReadOnlyCollection<string> roles, CancellationToken ct = default);
    }
}
