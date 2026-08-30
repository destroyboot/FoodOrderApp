using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;
        private readonly IPushNotificationService _pushNotifications;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repo,
            IAsyncQueryExecutor q,
            IUnitOfWork uow,
            IPushNotificationService pushNotifications,
            ILogger<NotificationService> logger)
        {
            _repo = repo;
            _q = q;
            _uow = uow;
            _pushNotifications = pushNotifications;
            _logger = logger;
        }

        public async Task CreateAsync(
            string ownerKey,
            NotificationType type,
            string title,
            string body,
            string? payloadJson = null,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(ownerKey))
                throw new InvalidOperationException("OwnerKey is required.");

            var notification = new Notification
            {
                OwnerKey = ownerKey,
                Type = type,
                Title = title,
                Body = body,
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(notification, ct);
            await _uow.SaveChangesAsync(ct);

            try
            {
                await _pushNotifications.SendToOwnerAsync(ownerKey, title, body, payloadJson, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notification delivery failed for owner {OwnerKey}.", ownerKey);
            }
        }

        public async Task MarkReadAsync(string ownerKey, int notificationId, CancellationToken ct = default)
        {
            var notification = await _q.FirstOrDefaultAsync(
                _repo.Query(tracked: true).Where(x => x.Id == notificationId && x.OwnerKey == ownerKey),
                ct);

            if (notification is null) throw new KeyNotFoundException("Notification not found.");

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
        }

        public async Task MarkAllReadAsync(string ownerKey, CancellationToken ct = default)
        {
            var unread = await _q.ToListAsync(
                _repo.Query(tracked: true).Where(x => x.OwnerKey == ownerKey && !x.IsRead),
                ct);

            if (unread.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var notification in unread)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _uow.SaveChangesAsync(ct);
        }
    }
}
