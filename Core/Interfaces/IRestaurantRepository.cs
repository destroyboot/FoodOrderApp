using Core.Data.Entities;

namespace Core.Interfaces
{
    public interface IRestaurantRepository : IQueryRepository<Restaurant>, ICommandRepository<Restaurant>
    {
    }
}
