using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Storage;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Planning;

public class DailyFoodPlanService : IDailyFoodPlanService
{
    public List<DailyFoodPlan> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.DailyFoodPlans
            .OrderBy(p => p.CreatedAt)
            .ToList();
    }

    public DailyFoodPlan? Get(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.DailyFoodPlans.Find(id);
    }

    public DailyFoodPlan Add(DailyFoodPlan plan)
    {
        plan.Id = Guid.NewGuid().ToString();
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = null;

        using var context = BroccoliDbContext.CreateForApp();
        context.DailyFoodPlans.Add(plan);
        context.SaveChanges();
        NotifyStorageChanged();
        return plan;
    }

    public DailyFoodPlan Update(DailyFoodPlan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.DailyFoodPlans.Update(plan);
        context.SaveChanges();
        NotifyStorageChanged();
        return plan;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        DailyFoodPlan? plan = context.DailyFoodPlans.Find(id);
        if (plan is not null)
        {
            context.DailyFoodPlans.Remove(plan);
            context.SaveChanges();
        }

        NotifyStorageChanged();
    }

    private static void NotifyStorageChanged() =>
        WeakReferenceMessenger.Default.Send(new StorageChangedMessage());
}
