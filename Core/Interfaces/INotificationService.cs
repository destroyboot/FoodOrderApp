using Core.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface INotificationService
    {
        Task CreateAsync(
            string ownerKey,
            NotificationType type,
            string title,
            string body,
            string? payloadJson = null,
            CancellationToken ct = default);

        Task MarkReadAsync(string ownerKey, int notificationId, CancellationToken ct = default);
        Task MarkAllReadAsync(string ownerKey, CancellationToken ct = default);
    }
}
