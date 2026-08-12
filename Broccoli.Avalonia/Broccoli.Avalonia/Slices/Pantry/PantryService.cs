using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Storage;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Pantry;

public class PantryService : IPantryService
{
    public List<PantryItem> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        return context.PantryItems
            .OrderBy(pantryItem => pantryItem.CreatedAt)
            .ToList();
    }

    public PantryItem Add(PantryItem item)
    {
        item.Id = Guid.NewGuid().ToString();
        item.CreatedAt = DateTime.UtcNow;

        using var context = BroccoliDbContext.CreateForApp();
        context.PantryItems.Add(item);
        context.SaveChanges();

        WeakReferenceMessenger.Default.Send(new PantryListChangedMessage());
        return item;
    }

    public PantryItem Update(PantryItem item)
    {
        using var context = BroccoliDbContext.CreateForApp();
        context.PantryItems.Update(item);
        context.SaveChanges();

        WeakReferenceMessenger.Default.Send(new PantryListChangedMessage());
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

        WeakReferenceMessenger.Default.Send(new PantryListChangedMessage());
    }

    public PantryItem? FindByName(string itemName)
    {
        using var context = BroccoliDbContext.CreateForApp();
        string lowerName = itemName.ToLowerInvariant();
        return context.PantryItems
            .AsEnumerable()
            .FirstOrDefault(i =>
                i.Name.ToLower().Contains(lowerName) ||
                lowerName.Contains(i.Name.ToLower()));
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
