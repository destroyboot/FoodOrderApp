using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IAsyncQueryExecutor
    {
        Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default);
        Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct = default);
        Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default);
        Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct = default);
    }
}
