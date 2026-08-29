using Core.Data.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RestaurantTableRepository : IRestaurantTableRepository
    {
        private readonly AppDbContext _db;

        public RestaurantTableRepository(AppDbContext db) => _db = db;

        public IQueryable<RestaurantTable> Query(bool tracked = false)
        {
            var query = _db.RestaurantTables.AsQueryable();
            return tracked ? query : query.AsNoTracking();
        }

        public Task AddAsync(RestaurantTable entity, CancellationToken ct = default)
            => _db.RestaurantTables.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(RestaurantTable entity, CancellationToken ct = default)
        {
            _db.RestaurantTables.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(RestaurantTable entity, CancellationToken ct = default)
        {
            _db.RestaurantTables.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
