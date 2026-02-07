using Core.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IMenuCategoryRepository : IQueryRepository<MenuCategory>, ICommandRepository<MenuCategory>
    {
    }
}
