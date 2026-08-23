using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.Platform;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Storage;
using Microsoft.EntityFrameworkCore;

namespace Broccoli.Avalonia.IngredientParsing;

/// <summary>
/// Reads the seed food database from the embedded <c>FoodDatabase.json</c> and writes it into
/// SQLite. The JSON is used only to initialise/reset the database, never as the live store.
/// </summary>
public static class FoodDatabaseSeeder
{
    private static readonly Uri SeedUri = new("avares://Broccoli.Avalonia/Assets/FoodDatabase.json");

    public static List<Food> ReadSeedFoods()
    {
        try
        {
            using Stream stream = AssetLoader.Open(SeedUri);
            using var reader = new StreamReader(stream);
            string jsonContent = reader.ReadToEnd();

            List<Food>? foods = JsonSerializer.Deserialize<List<Food>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (foods is null)
            {
                return [];
            }

            foreach (Food food in foods)
            {
                food.IsCustom = false;
            }

            return foods.Where(f => !string.IsNullOrWhiteSpace(f.Name)).ToList();
        }
        catch (Exception)
        {
            // Design-time or missing resource — return an empty seed.
            return [];
        }
    }

    public static void SeedIfEmpty(BroccoliDbContext context)
    {
        if (context.Foods.Any())
        {
            return;
        }

        context.Foods.AddRange(ReadSeedFoods());
        context.SaveChanges();
    }

    public static void Reset(BroccoliDbContext context)
    {
        context.Foods.ExecuteDelete();
        context.Foods.AddRange(ReadSeedFoods());
        context.SaveChanges();
    }
}
