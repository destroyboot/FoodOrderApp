using Core.Data.Entities;
using Core.Data.Enums;
using Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repo;
        private readonly IAsyncQueryExecutor _q;
        private readonly IUnitOfWork _uow;

        public NotificationService(INotificationRepository repo, IAsyncQueryExecutor q, IUnitOfWork uow)
        {
            _repo = repo;
            _q = q;
            _uow = uow;
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

            var n = new Notification
            {
                OwnerKey = ownerKey,
                Type = type,
                Title = title,
                Body = body,
                PayloadJson = payloadJson,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(n, ct);
            await _uow.SaveChangesAsync(ct);
        }

        public async Task MarkReadAsync(string ownerKey, int notificationId, CancellationToken ct = default)
        {
            var n = await _q.FirstOrDefaultAsync(
                _repo.Query(tracked: true).Where(x => x.Id == notificationId && x.OwnerKey == ownerKey),
                ct);

            if (n is null) throw new KeyNotFoundException("Notification not found.");

            if (!n.IsRead)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
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
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _uow.SaveChangesAsync(ct);
        }
    }
}
