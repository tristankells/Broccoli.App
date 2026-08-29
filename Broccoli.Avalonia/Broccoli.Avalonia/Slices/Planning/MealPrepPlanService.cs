using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Storage;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Planning;

public class MealPrepPlanService : IMealPrepPlanService
{
    public List<MealPrepPlan> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.MealPrepPlans
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.CreatedAt)
            .ToList();
    }

    public MealPrepPlan Add(MealPrepPlan plan)
    {
        plan.Id = Guid.NewGuid().ToString();
        plan.CreatedAt = DateTime.UtcNow;
        plan.UpdatedAt = null;

        using var context = BroccoliDbContext.CreateForApp();
        context.MealPrepPlans.Add(plan);
        context.SaveChanges();
        NotifyStorageChanged();
        return plan;
    }

    public MealPrepPlan Update(MealPrepPlan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.MealPrepPlans.Update(plan);
        context.SaveChanges();
        NotifyStorageChanged();
        return plan;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        MealPrepPlan? plan = context.MealPrepPlans.Find(id);
        if (plan is not null)
        {
            context.MealPrepPlans.Remove(plan);
            context.SaveChanges();
        }

        NotifyStorageChanged();
    }

    public void Reorder(List<string> orderedPlanIds)
    {
        using var context = BroccoliDbContext.CreateForApp();
        var plans = context.MealPrepPlans.ToList();
        for (int i = 0; i < orderedPlanIds.Count; i++)
        {
            MealPrepPlan? plan = plans.FirstOrDefault(p => p.Id == orderedPlanIds[i]);
            if (plan is not null)
            {
                plan.SortOrder = i;
            }
        }

        context.SaveChanges();
        NotifyStorageChanged();
    }

    private static void NotifyStorageChanged() =>
        WeakReferenceMessenger.Default.Send(new StorageChangedMessage());
}
