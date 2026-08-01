using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Groceries;

public interface IGroceryListService
{
    List<GroceryListItem> GetAll();
    GroceryListItem Add(GroceryListItem item);
    void AddMultiple(IEnumerable<GroceryListItem> items);
    GroceryListItem Update(GroceryListItem item);
    void Delete(string id);
    void Reset();
}
