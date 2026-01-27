using System.Text.Json.Serialization;

namespace Broccoli.Data.Models
{
    public class Recipe
    {
        /// <summary>
        /// Unique identifier of the recipe
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the recipe
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Long text block of newline-separated recipe ingredients
        /// </summary>
        [JsonPropertyName("ingredients")]
        public string Ingredients { get; set; } = string.Empty;

        /// <summary>
        /// Long unformatted text block of recipe directions
        /// </summary>
        [JsonPropertyName("directions")]
        public string Directions { get; set; } = string.Empty;

        /// <summary>
        /// Any additional notes from the chef, might include cooking tips or alternative ingredients
        /// </summary>
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Intended number of servings
        /// </summary>
        [JsonPropertyName("servings")]
        public int? Servings { get; set; }

        /// <summary>
        /// Time to prep in minutes
        /// </summary>
        [JsonPropertyName("prepTimeMinutes")]
        public int? PrepTimeMinutes { get; set; }

        /// <summary>
        /// Time to cook in minutes
        /// </summary>
        [JsonPropertyName("cookTimeMinutes")]
        public int? CookTimeMinutes { get; set; }

        /// <summary>
        /// Where the recipe is from
        /// </summary>
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        /// <summary>
        /// Optional URL if the recipe was obtained from online
        /// </summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>
        /// List of tags describing the recipe (e.g., Beef, Chicken, Freezable)
        /// </summary>
        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// List of image URLs or paths associated with the recipe
        /// </summary>
        [JsonPropertyName("images")]
        public List<string> Images { get; set; } = new();

        /// <summary>
        /// ID of the user who owns the recipe (partition key for CosmosDB)
        /// </summary>
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the recipe was created
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the recipe was last updated
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}