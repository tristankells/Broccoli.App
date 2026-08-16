using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Planning;

public interface IMealPrepPlanService
{
    List<MealPrepPlan> GetAll();

    MealPrepPlan Add(MealPrepPlan plan);

    MealPrepPlan Update(MealPrepPlan plan);

    void Delete(string id);

    void Reorder(List<string> orderedPlanIds);
}
