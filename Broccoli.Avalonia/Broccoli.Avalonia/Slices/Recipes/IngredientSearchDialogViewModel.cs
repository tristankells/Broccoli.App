using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class IngredientSearchDialogViewModel : ViewModelBase
{
    private readonly IRecipeIngredientSearchService _searchService;

    public IngredientSearchDialogViewModel(IRecipeIngredientSearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>Comma, semicolon or line separated foods the user wants to use up.</summary>
    [ObservableProperty]
    private string _searchTermsText = string.Empty;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _hasSearched;

    public ObservableCollection<RecipeIngredientSearchResultViewModel> Results { get; } = [];

    public bool HasResults => Results.Count > 0;

    public bool HasNoResults => HasSearched && !HasResults;

    public Action<Recipe>? RecipeSelected { get; set; }

    public Action? RequestClose { get; set; }

    [RelayCommand]
    private async Task SearchAsync()
    {
        List<string> terms = SplitTerms(SearchTermsText);
        if (terms.Count == 0)
        {
            return;
        }

        IsSearching = true;
        try
        {
            IReadOnlyList<RecipeIngredientSearchResult> results =
                await Task.Run(() => _searchService.Search(terms));

            Results.Clear();
            foreach (RecipeIngredientSearchResult result in results)
            {
                Results.Add(new RecipeIngredientSearchResultViewModel(result));
            }
        }
        finally
        {
            IsSearching = false;
            HasSearched = true;
        }
    }

    [RelayCommand]
    private void OpenRecipe(RecipeIngredientSearchResultViewModel result)
    {
        RecipeSelected?.Invoke(result.Recipe);
        RequestClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke();

    private static List<string> SplitTerms(string text) =>
        text.Split([',', ';', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}

public class RecipeIngredientSearchResultViewModel
{
    public RecipeIngredientSearchResultViewModel(RecipeIngredientSearchResult result)
    {
        Recipe = result.Recipe;
        Name = result.Recipe.Name;
        MatchCountText = $"{result.MatchCount} of {result.TotalTerms} matched";
        AveragePercentText = $"{result.AverageMatchPercent:0}% match";
        MatchedIngredients = result.MatchedIngredients
            .Select(hit => new IngredientHitViewModel(hit))
            .ToList();
    }

    public Recipe Recipe { get; }

    public string Name { get; }

    public string MatchCountText { get; }

    public string AveragePercentText { get; }

    public IReadOnlyList<IngredientHitViewModel> MatchedIngredients { get; }
}

public class IngredientHitViewModel
{
    public IngredientHitViewModel(IngredientHit hit)
    {
        IngredientLine = hit.IngredientLine;
        SearchTerm = hit.SearchTerm;
        Method = hit.Method;
        ScorePercentText = $"{hit.ScorePercent:0}%";
        ScoreColor = hit.ScorePercent >= 80
            ? "#2ECC71"
            : hit.ScorePercent >= 60
                ? "#F39C12"
                : "#E74C3C";
    }

    public string IngredientLine { get; }

    public string SearchTerm { get; }

    public string Method { get; }

    public string ScorePercentText { get; }

    public string ScoreColor { get; }
}
