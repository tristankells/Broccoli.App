using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Recipes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class MealPrepViewModel : ViewModelBase
{
    private readonly IMealPrepPlanService _planService;
    private readonly IRecipeService _recipeService;

    public ObservableCollection<MealPrepPlan> Plans { get; } = new();

    [ObservableProperty] private string? _errorMessage;

    public MealPrepViewModel() : this(new MealPrepPlanService(), new RecipeService())
    {
    }

    public MealPrepViewModel(IMealPrepPlanService planService, IRecipeService recipeService)
    {
        _planService = planService;
        _recipeService = recipeService;
        Load();
    }

    public void Load()
    {
        ErrorMessage = null;
        try
        {
            List<MealPrepPlan> plans = _planService.GetAll();
            Plans.Clear();
            foreach (MealPrepPlan p in plans)
            {
                Plans.Add(p);
            }
        }
        catch (Exception ex) { ErrorMessage = $"Failed to load: {ex.Message}"; }
    }

    public IReadOnlyList<Recipe> AllRecipes => _recipeService.GetAll();

    public List<Recipe> GetRecipesForPlan(MealPrepPlan plan) =>
        plan.RecipeIds
            .Select(id => AllRecipes.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .Cast<Recipe>()
            .ToList();

    [RelayCommand]
    private void NewPlan()
    {
        try
        {
            var plan = new MealPrepPlan { Name = "New Plan" };
            plan = _planService.Add(plan);
            Plans.Add(plan);
        }
        catch (Exception ex) { ErrorMessage = $"Failed to create: {ex.Message}"; }
    }

    [RelayCommand]
    private void DeletePlan(MealPrepPlan plan)
    {
        try
        {
            _planService.Delete(plan.Id);
            Plans.Remove(plan);
        }
        catch (Exception ex) { ErrorMessage = $"Failed to delete: {ex.Message}"; }
    }

    [RelayCommand]
    private void SavePlan(MealPrepPlan plan)
    {
        try { _planService.Update(plan); }
        catch (Exception ex) { ErrorMessage = $"Failed to save: {ex.Message}"; }
    }

    [RelayCommand]
    private void ToggleRecipe(PlanRecipeArg arg)
    {
        if (arg.IsSelected)
        {
            arg.Plan.RecipeIds.Add(arg.Recipe.Id);
        }
        else
        {
            arg.Plan.RecipeIds.Remove(arg.Recipe.Id);
        }
    }
}

public class PlanRecipeArg
{
    public MealPrepPlan Plan { get; init; } = null!;
    public Recipe Recipe { get; init; } = null!;
    public bool IsSelected { get; set; }
}
