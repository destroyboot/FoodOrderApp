using Core.Data.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;

        public NotificationRepository(AppDbContext db) => _db = db;

        public IQueryable<Notification> Query(bool tracked = false)
            => tracked ? _db.Notifications : _db.Notifications.AsNoTracking();

        public Task AddAsync(Notification notification, CancellationToken ct = default)
            => _db.Notifications.AddAsync(notification, ct).AsTask();

        public void Remove(Notification notification)
            => _db.Notifications.Remove(notification);
    }
}
