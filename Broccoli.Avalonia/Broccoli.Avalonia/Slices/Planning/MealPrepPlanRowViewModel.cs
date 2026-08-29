using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Planning;

/// <summary>
/// A meal-prep plan with its seasonality summary precomputed: how many of the plan's recipes are
/// Green/Yellow/Red for today, plus a warning when the plan is dominated by out-of-season recipes.
/// </summary>
public partial class MealPrepPlanRowViewModel : ViewModelBase
{
    public MealPrepPlanRowViewModel(MealPrepPlan plan)
    {
        Plan = plan;
    }

    public MealPrepPlan Plan { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSeasonality))]
    private string _seasonalitySummary = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWarning))]
    private string? _seasonalityWarning;

    public bool HasSeasonality => !string.IsNullOrEmpty(SeasonalitySummary);

    public bool HasWarning => SeasonalityWarning is not null;
}
