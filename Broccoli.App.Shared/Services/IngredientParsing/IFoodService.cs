using Broccoli.Data.Models;

namespace Broccoli.Shared.Services
{
    /// <summary>
    /// The result of a multi-stage food matching operation.
    /// </summary>
    public class FoodMatchResult
    {
        /// <summary>The best matching food item, or null if nothing was found.</summary>
        public Food? Food { get; init; }

        /// <summary>
        /// Normalised similarity score in [0, 1].
        /// 1.0 = perfect match · 0.0 = no similarity.
        /// </summary>
        public double Score { get; init; }

        /// <summary>Which stage produced this result ("Exact", "Token", "Fuzzy", "FuzzySharp").</summary>
        public string Method { get; init; } = string.Empty;

        /// <summary>Whether any match was found.</summary>
        public bool IsMatch => Food != null;
    }

    public interface IFoodService
    {
        bool TryGetFood(string name, out Food food);
        bool TryGetFoodFuzzy(string name, out Food food);
        Task<IEnumerable<Food>> GetAllAsync();

        /// <summary>
        /// Runs a multi-stage matching pipeline (Exact → Token/Jaccard → Levenshtein → FuzzySharp)
        /// and returns the best candidate with a normalised score and method label.
        /// </summary>
        FoodMatchResult FindBestMatch(string foodDescription);

        // ── Write operations ────────────────────────────────────────────────

        /// <summary>Adds a new food, assigns the next available Id, and persists to disk.</summary>
        Task<Food> AddAsync(Food food);

        /// <summary>Updates an existing food (matched by Id) and persists to disk.</summary>
        Task UpdateAsync(Food food);

        /// <summary>Deletes the food with the given Id and persists to disk.</summary>
        Task DeleteAsync(int id);
    }
}