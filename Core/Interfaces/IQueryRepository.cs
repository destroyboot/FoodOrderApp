using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IQueryRepository<T>
    {
        IQueryable<T> Query(bool tracked = false);
    }
}
