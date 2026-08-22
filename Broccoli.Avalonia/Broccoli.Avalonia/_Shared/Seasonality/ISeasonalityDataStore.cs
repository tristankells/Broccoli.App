using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Seasonality;

/// <summary>
/// Read/write access to the editable produce seasonality dataset, persisted in SQLite and
/// seeded from the embedded <c>nz-produce.json</c>.
/// </summary>
public interface ISeasonalityDataStore
{
    /// <summary>Returns every produce item, seeding the dataset from the embedded JSON on first access.</summary>
    List<ProduceItem> GetAll();

    /// <summary>Returns a single produce item by id, or null when not found.</summary>
    ProduceItem? Get(string id);

    /// <summary>Adds a new produce item and returns it.</summary>
    ProduceItem Add(ProduceItem item);

    /// <summary>Updates an existing produce item and returns it.</summary>
    ProduceItem Update(ProduceItem item);

    /// <summary>Deletes a produce item by id.</summary>
    void Delete(string id);

    /// <summary>Restores the dataset to the embedded seed data, discarding edits and additions.</summary>
    void Reset();
}
