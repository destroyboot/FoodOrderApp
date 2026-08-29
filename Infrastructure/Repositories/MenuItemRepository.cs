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
    public class MenuItemRepository : IMenuItemRepository
    {
        private readonly AppDbContext _db;
        public MenuItemRepository(AppDbContext db) => _db = db;

        public IQueryable<MenuItem> Query(bool tracked = false)
        {
            var query = _db.MenuItems
            .Include(m => m.Translations)
            .Include(m => m.Category)
            .AsQueryable();
            return tracked ? query : query.AsNoTracking();
        }

        public Task AddAsync(MenuItem entity, CancellationToken ct = default)
            => _db.MenuItems.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(MenuItem entity, CancellationToken ct = default)
        {
            _db.MenuItems.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(MenuItem entity, CancellationToken ct = default)
        {
            _db.MenuItems.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
