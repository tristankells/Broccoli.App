using Broccoli.Data.Models;

namespace Broccoli.Shared.Services
{
    public interface IFoodService
    {
        bool TryGetFood(string name, out Food food);
        Task<IEnumerable<Food>> GetAllAsync();
    }
}