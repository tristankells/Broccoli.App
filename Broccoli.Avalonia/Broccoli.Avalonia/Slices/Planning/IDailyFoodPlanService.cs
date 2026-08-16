using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Planning;

public interface IDailyFoodPlanService
{
    List<DailyFoodPlan> GetAll();

    DailyFoodPlan? Get(string id);

    DailyFoodPlan Add(DailyFoodPlan plan);

    DailyFoodPlan Update(DailyFoodPlan plan);

    void Delete(string id);
}
