﻿using System.Text.Json;
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

    /// <summary>
    /// Attempts to retrieve a food item by fuzzy matching using Levenshtein distance algorithm.
    /// Performs case-insensitive matching with typo tolerance.
    /// </summary>
    /// <param name="name">The name of the food to search for</param>
    /// <param name="maxDistance">Maximum Levenshtein distance threshold (typically 3 for typo tolerance)</param>
    /// <param name="food">When this method returns true, contains the best matching food item; otherwise, null</param>
    /// <returns>True if a food item within maxDistance was found; otherwise, false</returns>
    public bool TryGetFoodFuzzy(string name, int maxDistance, out Food food)
    {
        food = null;

        if (string.IsNullOrWhiteSpace(name) || _foodByName.Count == 0)
        {
            return false;
        }

        string searchName = name.Trim().ToLowerInvariant();
        int bestDistance = int.MaxValue;
        Food? bestMatch = null;

        foreach (var candidate in _foodByName.Values)
        {
            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                continue;
            }

            int distance = CalculateLevenshteinDistance(searchName, candidate.Name.ToLowerInvariant());

            if (distance <= maxDistance && distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = candidate;

                // Perfect match found, return immediately
                if (distance == 0)
                {
                    food = bestMatch;
                    return true;
                }
            }
        }

        if (bestMatch != null && bestDistance <= maxDistance)
        {
            food = bestMatch;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// This measures the minimum number of single-character edits (insertions, deletions, substitutions)
    /// required to change one string into another.
    /// </summary>
    /// <param name="source">The source string</param>
    /// <param name="target">The target string</param>
    /// <returns>The Levenshtein distance</returns>
    private static int CalculateLevenshteinDistance(string source, string target)
    {
        if (source == target)
        {
            return 0;
        }

        int sourceLength = source.Length;
        int targetLength = target.Length;

        if (sourceLength == 0)
        {
            return targetLength;
        }

        if (targetLength == 0)
        {
            return sourceLength;
        }

        // Create two rows for space-optimized DP calculation (we only need previous and current row)
        int[] previousRow = new int[targetLength + 1];
        int[] currentRow = new int[targetLength + 1];

        // Initialize first row
        for (int j = 0; j <= targetLength; j++)
        {
            previousRow[j] = j;
        }

        for (int i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (int j = 1; j <= targetLength; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        previousRow[j] + 1,      // Deletion
                        currentRow[j - 1] + 1),  // Insertion
                    previousRow[j - 1] + cost);  // Substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }
}
