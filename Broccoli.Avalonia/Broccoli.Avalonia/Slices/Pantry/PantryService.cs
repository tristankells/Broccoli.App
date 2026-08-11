using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Slices.Pantry;

public class PantryService : IPantryService
{
    public List<PantryItem> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.PantryItems
            .OrderBy(i => i.CreatedAt)
            .ToList();
    }

    public PantryItem Add(PantryItem item)
    {
        item.Id = Guid.NewGuid().ToString();
        item.CreatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.PantryItems.Add(item);
        context.SaveChanges();
        return item;
    }

    public PantryItem Update(PantryItem item)
    {
        using var context = BroccoliDbContext.CreateForApp();
        context.PantryItems.Update(item);
        context.SaveChanges();
        return item;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        PantryItem? item = context.PantryItems.Find(id);
        if (item is not null)
        {
            context.PantryItems.Remove(item);
            context.SaveChanges();
        }
    }

    public bool Exists(string itemName)
    {
        using var context = BroccoliDbContext.CreateForApp();
        string lowerName = itemName.ToLowerInvariant();
        return context.PantryItems.Any(i =>
            i.Name.ToLower().Contains(lowerName) ||
            lowerName.Contains(i.Name.ToLower()));
    }
}
