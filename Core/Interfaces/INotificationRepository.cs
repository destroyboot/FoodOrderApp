using Core.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface INotificationRepository
    {
        IQueryable<Notification> Query(bool tracked = false);
        Task AddAsync(Notification notification, CancellationToken ct = default);
        void Remove(Notification notification);
    }
}
