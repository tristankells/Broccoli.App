using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Storage;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// SQLite-backed store for the produce seasonality dataset. Reads are seeded lazily from the
/// embedded <c>nz-produce.json</c> so the dataset is always available on first use.
/// </summary>
public class SeasonalityDataStore : ISeasonalityDataStore
{
    public List<ProduceItem> GetAll()
    {
        using var context = BroccoliDbContext.CreateForApp();
        ProduceSeeder.SeedIfEmpty(context);
        return context.ProduceItems
            .OrderBy(p => p.Name)
            .ToList();
    }

    public ProduceItem? Get(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        ProduceSeeder.SeedIfEmpty(context);
        return context.ProduceItems.Find(id);
    }

    public ProduceItem Add(ProduceItem item)
    {
        using var context = BroccoliDbContext.CreateForApp();
        context.ProduceItems.Add(item);
        context.SaveChanges();
        NotifyChanged();
        return item;
    }

    public ProduceItem Update(ProduceItem item)
    {
        using var context = BroccoliDbContext.CreateForApp();
        context.ProduceItems.Update(item);
        context.SaveChanges();
        NotifyChanged();
        return item;
    }

    public void Delete(string id)
    {
        using var context = BroccoliDbContext.CreateForApp();
        ProduceItem? item = context.ProduceItems.Find(id);
        if (item is not null)
        {
            context.ProduceItems.Remove(item);
            context.SaveChanges();
            NotifyChanged();
        }
    }

    public void Reset()
    {
        using var context = BroccoliDbContext.CreateForApp();
        ProduceSeeder.Reset(context);
        NotifyChanged();
    }

    private static void NotifyChanged()
    {
        WeakReferenceMessenger.Default.Send(new SeasonalityDataChangedMessage());
        WeakReferenceMessenger.Default.Send(new StorageChangedMessage());
    }
}
