using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class PlanningPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _currentTabIndex;

    public PlanningPageViewModel()
        : this(
        new MacroTargetsViewModel(),
        new DayPlanViewModel(),
        new MealPrepViewModel())
    {
    }

    public PlanningPageViewModel(
        MacroTargetsViewModel macroTargets,
        DayPlanViewModel dayPlans,
        MealPrepViewModel mealPrep)
    {
        MacroTargets = macroTargets;
        DayPlans = dayPlans;
        MealPrep = mealPrep;
    }

    public MacroTargetsViewModel MacroTargets { get; }

    public DayPlanViewModel DayPlans { get; }

    public MealPrepViewModel MealPrep { get; }
}
