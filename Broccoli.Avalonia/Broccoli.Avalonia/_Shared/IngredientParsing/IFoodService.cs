using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.IngredientParsing;

public class FoodMatchResult
{
    public Food? Food { get; init; }
    public double Score { get; init; }
    public string Method { get; init; } = string.Empty;
    public bool IsMatch => Food != null;
}

public interface IFoodService
{
    bool TryGetFood(string name, out Food food);
    bool TryGetFoodFuzzy(string name, out Food food);
    List<Food> GetAll();
    FoodMatchResult FindBestMatch(string foodDescription);
    Food Add(Food food);
    void Update(Food food);
    void Delete(int id);
}
