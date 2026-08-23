using System.Text.Json;
using Broccoli.Avalonia.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Broccoli.Avalonia.Storage;

/// <summary>
/// Local SQLite store for the "structured" slices of app data (everything except Recipes,
/// which are stored as Markdown files + images via <see cref="IRecipeMarkdownStore"/>).
/// One file: <see cref="AppPaths.DatabaseFilePath"/>.
/// </summary>
public class BroccoliDbContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (a, b) => (a ?? new()).SequenceEqual(b ?? new()),
        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
        v => v.ToList());

    private static readonly ValueComparer<List<DailyFoodPlanTab>> TabsListComparer = new(
        (a, b) => JsonSerializer.Serialize(a, JsonOptions) == JsonSerializer.Serialize(b, JsonOptions),
        v => JsonSerializer.Serialize(v, JsonOptions).GetHashCode(),
        v => JsonSerializer.Deserialize<List<DailyFoodPlanTab>>(JsonSerializer.Serialize(v, JsonOptions), JsonOptions)!);

    private static readonly ValueComparer<Dictionary<int, SeasonalityState>> MonthStateDictComparer = new(
        (a, b) => (a ?? new Dictionary<int, SeasonalityState>()).Count == (b ?? new Dictionary<int, SeasonalityState>()).Count
            && (a ?? new Dictionary<int, SeasonalityState>()).Keys.All(k =>
                (b ?? new Dictionary<int, SeasonalityState>()).ContainsKey(k)
                && (b ?? new Dictionary<int, SeasonalityState>())[k] == (a ?? new Dictionary<int, SeasonalityState>())[k]),
        v => (v ?? new Dictionary<int, SeasonalityState>()).Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
        v => v == null ? new Dictionary<int, SeasonalityState>() : new Dictionary<int, SeasonalityState>(v));

    public BroccoliDbContext(DbContextOptions<BroccoliDbContext> options)
        : base(options)
    {
    }

    public DbSet<GroceryListItem> GroceryListItems => Set<GroceryListItem>();

    public DbSet<PantryItem> PantryItems => Set<PantryItem>();

    public DbSet<MealPrepPlan> MealPrepPlans => Set<MealPrepPlan>();

    public DbSet<MacroTarget> MacroTargets => Set<MacroTarget>();

    public DbSet<MacroTargetSettings> MacroTargetSettings => Set<MacroTargetSettings>();

    public DbSet<DailyFoodPlan> DailyFoodPlans => Set<DailyFoodPlan>();

    public DbSet<Food> Foods => Set<Food>();

    public DbSet<ProduceItem> ProduceItems => Set<ProduceItem>();

    /// <summary>
    /// Convenience factory for runtime (non-design-time) code, pointing at the app's real
    /// local database file (<see cref="AppPaths.DatabaseFilePath"/>).
    /// </summary>
    public static BroccoliDbContext CreateForApp()
    {
        DbContextOptions<BroccoliDbContext> options = new DbContextOptionsBuilder<BroccoliDbContext>()
            .UseSqlite($"Data Source={AppPaths.DatabaseFilePath}")
            .Options;

        return new BroccoliDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GroceryListItem>().Ignore(x => x.IsEditing);
        modelBuilder.Entity<GroceryListItem>().Ignore(x => x.EditText);

        // Food.Id is seeded from the embedded JSON, so EF must never assign it.
        modelBuilder.Entity<Food>()
            .Property(f => f.Id)
            .ValueGeneratedNever();

        // ProduceItem.Id is seeded from the embedded JSON, so EF must never assign it.
        modelBuilder.Entity<ProduceItem>()
            .Property(p => p.Id)
            .ValueGeneratedNever();

        // ProduceItem.Months (month -> state) is a dictionary -> store as a JSON-encoded column.
        modelBuilder.Entity<ProduceItem>()
            .Property(p => p.Months)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<int, SeasonalityState>>(v, JsonOptions) ?? new Dictionary<int, SeasonalityState>())
            .Metadata.SetValueComparer(MonthStateDictComparer);

        // MealPrepPlan.RecipeIds is a simple string list -> store as a JSON-encoded column.
        modelBuilder.Entity<MealPrepPlan>()
            .Property(p => p.RecipeIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, JsonOptions) ?? new List<string>())
            .Metadata.SetValueComparer(StringListComparer);

        // DailyFoodPlan.Tabs is a nested object graph (tabs -> rows) -> store as a JSON-encoded column
        // rather than normalising into child tables, since it is always read/written as a whole document.
        modelBuilder.Entity<DailyFoodPlan>()
            .Property(p => p.Tabs)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<List<DailyFoodPlanTab>>(v, JsonOptions) ?? new List<DailyFoodPlanTab>())
            .Metadata.SetValueComparer(TabsListComparer);

        base.OnModelCreating(modelBuilder);
    }
}
