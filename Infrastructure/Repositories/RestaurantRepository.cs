using Core.Data.Entities;
using Core.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class RestaurantRepository : IRestaurantRepository
    {
        private readonly AppDbContext _db;

        public RestaurantRepository(AppDbContext db) => _db = db;

        public IQueryable<Restaurant> Query(bool tracked = false)
        {
            var query = _db.Restaurants
                .Include(x => x.Settings)
                .Include(x => x.Tables)
                .Include(x => x.UserRoles)
                .AsQueryable();

            return tracked ? query : query.AsNoTracking();
        }

        public Task AddAsync(Restaurant entity, CancellationToken ct = default)
            => _db.Restaurants.AddAsync(entity, ct).AsTask();

        public Task UpdateAsync(Restaurant entity, CancellationToken ct = default)
        {
            _db.Restaurants.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Restaurant entity, CancellationToken ct = default)
        {
            _db.Restaurants.Remove(entity);
            return Task.CompletedTask;
        }
    }
}
