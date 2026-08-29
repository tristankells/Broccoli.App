using System.Collections.ObjectModel;
using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Seasonality;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Recipes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class MealPrepViewModel : ViewModelBase
{
    private readonly IMealPrepPlanService _planService;
    private readonly IRecipeService _recipeService;
    private readonly IngredientParserService? _parser;
    private readonly ISeasonalityService? _seasonality;

    [ObservableProperty]
    private string? _errorMessage;

    public MealPrepViewModel()
        : this(new MealPrepPlanService(), new RecipeService(), null, null)
    {
    }

    public MealPrepViewModel(
        IMealPrepPlanService planService,
        IRecipeService recipeService,
        IngredientParserService? parser = null,
        ISeasonalityService? seasonality = null)
    {
        _planService = planService;
        _recipeService = recipeService;
        _parser = parser;
        _seasonality = seasonality;
        Load();
    }

    public ObservableCollection<MealPrepPlanRowViewModel> Plans { get; } = new();

    public IReadOnlyList<Recipe> AllRecipes => _recipeService.GetAll();

    public void Load()
    {
        ErrorMessage = null;
        try
        {
            List<MealPrepPlan> plans = _planService.GetAll();
            Plans.Clear();
            foreach (MealPrepPlan p in plans)
            {
                Plans.Add(new MealPrepPlanRowViewModel(p));
            }

            RefreshSeasonality();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    public List<Recipe> GetRecipesForPlan(MealPrepPlan plan) =>
        plan.RecipeIds
            .Select(id => AllRecipes.FirstOrDefault(r => r.Id == id))
            .Where(r => r is not null)
            .Cast<Recipe>()
            .ToList();

    /// <summary>
    /// Recomputes each plan's seasonality summary by scoring its recipes for today. A plan
    /// dominated by Red recipes gets a "mostly out of season" warning to steer ingredient swaps.
    /// </summary>
    public void RefreshSeasonality()
    {
        if (_parser is null || _seasonality is null)
        {
            return;
        }

        foreach (MealPrepPlanRowViewModel row in Plans)
        {
            int green = 0, yellow = 0, red = 0;
            foreach (Recipe recipe in GetRecipesForPlan(row.Plan))
            {
                if (string.IsNullOrWhiteSpace(recipe.Ingredients))
                {
                    continue;
                }

                SeasonalityResult result = _seasonality.Score(_parser.ParseAndMatchIngredients(recipe.Ingredients));
                switch (result.Band)
                {
                    case SeasonalityBand.Green:
                        green++;
                        break;
                    case SeasonalityBand.Yellow:
                        yellow++;
                        break;
                    case SeasonalityBand.Red:
                        red++;
                        break;
                }
            }

            List<string> parts = [];
            if (green > 0)
            {
                parts.Add($"{green} green");
            }

            if (yellow > 0)
            {
                parts.Add($"{yellow} yellow");
            }

            if (red > 0)
            {
                parts.Add($"{red} red");
            }

            row.SeasonalitySummary = parts.Count > 0 ? string.Join(" · ", parts) : string.Empty;
            row.SeasonalityWarning = red > 0 && red >= green ? "Mostly out of season" : null;
        }
    }

    [RelayCommand]
    private void NewPlan()
    {
        try
        {
            var plan = new MealPrepPlan { Name = "New Plan" };
            plan = _planService.Add(plan);
            Plans.Add(new MealPrepPlanRowViewModel(plan));
            RefreshSeasonality();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeletePlan(MealPrepPlan plan)
    {
        try
        {
            _planService.Delete(plan.Id);
            MealPrepPlanRowViewModel? row = Plans.FirstOrDefault(r => r.Plan.Id == plan.Id);
            if (row is not null)
            {
                Plans.Remove(row);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SavePlan(MealPrepPlan plan)
    {
        try
        {
            _planService.Update(plan);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
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

        RefreshSeasonality();
    }
}
