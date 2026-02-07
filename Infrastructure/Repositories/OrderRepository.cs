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
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _db;

        public OrderRepository(AppDbContext db)
        {
            _db = db;
        }

        public IQueryable<Order> Query(bool tracked = false)
        {
            var query = _db.Orders
                .Include(o => o.Items)
                .AsQueryable();

            return tracked ? query : query.AsNoTracking();
        }

        public Task AddAsync(Order entity, CancellationToken ct = default)
            => _db.Orders.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(Order entity, CancellationToken ct = default)
        {
            _db.Orders.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Order entity, CancellationToken ct = default)
        {
            _db.Orders.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
