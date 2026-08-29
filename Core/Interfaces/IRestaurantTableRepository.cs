using Core.Data.Entities;

namespace Core.Interfaces
{
    public interface IRestaurantTableRepository : IQueryRepository<RestaurantTable>, ICommandRepository<RestaurantTable>
    {
    }
}
