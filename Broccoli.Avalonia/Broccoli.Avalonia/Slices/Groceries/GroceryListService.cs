using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Groceries;

public class GroceryListService : IGroceryListService
{
    public List<GroceryListItem> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.GroceryListItems
            .OrderByDescending(i => i.CreatedAt)
            .ToList();
    }

    public GroceryListItem Add(GroceryListItem item)
    {
        item.Id = Guid.NewGuid().ToString();
        item.CreatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.GroceryListItems.Add(item);
        context.SaveChanges();
        return item;
    }

    public void AddMultiple(IEnumerable<GroceryListItem> items)
    {
        using var context = BroccoliDbContext.CreateForApp();
        foreach (GroceryListItem item in items)
        {
            item.Id = Guid.NewGuid().ToString();
            item.CreatedAt = DateTime.UtcNow;
            context.GroceryListItems.Add(item);
        }

        context.SaveChanges();
    }

    public GroceryListItem Update(GroceryListItem item)
    {
        using var context = BroccoliDbContext.CreateForApp();
        context.GroceryListItems.Update(item);
        context.SaveChanges();
        return item;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        GroceryListItem? item = context.GroceryListItems.Find(id);
        if (item is not null)
        {
            context.GroceryListItems.Remove(item);
            context.SaveChanges();
        }
    }

    public void Reset()
    {
        using var context = BroccoliDbContext.CreateForApp();
        var all = context.GroceryListItems.ToList();
        context.GroceryListItems.RemoveRange(all);
        context.SaveChanges();
    }
}
