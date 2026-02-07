using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Querying
{
    public class EfAsyncQueryExecutor : IAsyncQueryExecutor
    {
        public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default)
            => EntityFrameworkQueryableExtensions.ToListAsync(query, ct);

        public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct = default)
            => EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query, ct);

        public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default)
            => EntityFrameworkQueryableExtensions.CountAsync(query, ct);

        public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct = default)
            => EntityFrameworkQueryableExtensions.AnyAsync(query, ct);
    }
}
