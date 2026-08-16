using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Pantry;

public interface IPantryService
{
    List<PantryItem> GetAll();

    PantryItem Add(PantryItem item);

    PantryItem Update(PantryItem item);

    void Delete(string id);

    bool Exists(string itemName);

    PantryItem? FindByName(string itemName);
}
