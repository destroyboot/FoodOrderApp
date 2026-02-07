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
    public class MenuCategoryRepository : IMenuCategoryRepository
    {
        private readonly AppDbContext _db;

        public MenuCategoryRepository(AppDbContext db)
        {
            _db = db;
        }

        public IQueryable<MenuCategory> Query(bool tracked = false)
        {
            var query = _db.MenuCategories
                .Include(c => c.Translations)
                .AsQueryable();

            return tracked ? query : query.AsNoTracking();
        }

        public Task AddAsync(MenuCategory entity, CancellationToken ct = default)
            => _db.MenuCategories.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(MenuCategory entity, CancellationToken ct = default)
        {
            _db.MenuCategories.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(MenuCategory entity, CancellationToken ct = default)
        {
            _db.MenuCategories.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
