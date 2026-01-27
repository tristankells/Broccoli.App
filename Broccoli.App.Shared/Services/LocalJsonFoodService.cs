using System.Text.Json;
using Broccoli.Data.Models;
using Broccoli.Shared.Services;

namespace Ginger.Data.Services;

/// <summary>
/// High-performance food lookup service that loads the food database once
/// and uses an in-memory dictionary for O(1) lookups by name.
/// </summary>
public class LocalJsonFoodService : IFoodService
{
    private readonly Dictionary<string, Food> _foodByName;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };
    
    /// <summary>
    /// Initializes a new instance of the FoodService class.
    /// Loads the food database from the specified JSON file path.
    /// </summary>
    /// <param name="databasePath">The file path to the FoodDatabase.json file</param>
    public LocalJsonFoodService(string databasePath)
    {
        // Load the JSON file and build the dictionary once during initialization
        string jsonContent = File.ReadAllText(databasePath);
        var foods = JsonSerializer.Deserialize<List<Food>>(jsonContent, _jsonOptions);
        
        // Build a case-insensitive dictionary for fast O(1) lookups
        _foodByName = new Dictionary<string, Food>(StringComparer.OrdinalIgnoreCase);

        if (foods == null)
        {
            return;
        }

        foreach (Food food in foods.Where(food => !string.IsNullOrWhiteSpace(food.Name)))
        {
            // Store by name - first occurrence wins if duplicates exist
            _foodByName.TryAdd(food.Name, food);
        }
    }
    
    /// <summary>
    /// Attempts to retrieve a food item by name using case-insensitive matching.
    /// </summary>
    /// <param name="name">The name of the food to search for</param>
    /// <param name="food">When this method returns true, contains the matching food item; otherwise, null</param>
    /// <returns>True if a food item with the specified name was found; otherwise, false</returns>
    public bool TryGetFood(string name, out Food food)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return _foodByName.TryGetValue(name, out food);
        }

        food = null;
        return false;
    }
    
    /// <summary>
    /// Retrieves all food items from the database.
    /// </summary>
    /// <returns>A collection of all food items</returns>
    public Task<IEnumerable<Food>> GetAllAsync() => Task.FromResult<IEnumerable<Food>>(_foodByName.Values);
}
