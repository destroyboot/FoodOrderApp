using Core.Contracts.Orders;
using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IOrderService
    {
        Task<OrderPreviewResponseDto> PreviewAsync(OrderPreviewRequestDto dto, CancellationToken ct = default);
        Task<OrderCreateResponseDto> CreateAsync(OrderPreviewRequestDto dto, string userId, CancellationToken ct = default);

        Task ChangeStatusAsync(int orderId, OrderStatus newStatus, string userId, CancellationToken ct = default);
    }
}
