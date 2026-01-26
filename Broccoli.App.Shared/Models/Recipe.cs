using System.Text.Json.Serialization;

namespace Broccoli.Data.Models
{
    public class Recipe
    {
        /// <summary>
        /// Unique identifier of the recipe
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Name of the recipe
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Long text block of newline-separated recipe ingredients
        /// </summary>
        public string Ingredients { get; set; } = string.Empty;

        /// <summary>
        /// Long unformatted text block of recipe directions
        /// </summary>
        public string Directions { get; set; } = string.Empty;

        /// <summary>
        /// Any additional notes from the chef, might include cooking tips or alternative ingredients
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// Intended number of servings
        /// </summary>
        public int? Servings { get; set; }

        /// <summary>
        /// Time to prep in minutes
        /// </summary>
        public int? PrepTimeMinutes { get; set; }

        /// <summary>
        /// Time to cook in minutes
        /// </summary>
        public int? CookTimeMinutes { get; set; }

        /// <summary>
        /// Where the recipe is from
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Optional URL if the recipe was obtained from online
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// List of tags describing the recipe (e.g., Beef, Chicken, Freezable)
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// List of image URLs or paths associated with the recipe
        /// </summary>
        public List<string> Images { get; set; } = new();

        /// <summary>
        /// ID of the user who owns the recipe
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Timestamp when the recipe was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the recipe was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}