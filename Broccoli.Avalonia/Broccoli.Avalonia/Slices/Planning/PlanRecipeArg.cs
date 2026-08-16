using Broccoli.Avalonia.Models;

namespace Broccoli.Avalonia.Slices.Planning;

public class PlanRecipeArg
{
    public MealPrepPlan Plan { get; init; } = null!;

    public Recipe Recipe { get; init; } = null!;

    public bool IsSelected { get; set; }
}
