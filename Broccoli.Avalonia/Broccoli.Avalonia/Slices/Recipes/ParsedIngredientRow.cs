using Broccoli.Avalonia.IngredientParsing;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class ParsedIngredientRow : ViewModelBase
{
    [ObservableProperty]
    private bool _isMatched;
    [ObservableProperty]
    private string _foodName = string.Empty;
    [ObservableProperty]
    private string _foodDescription = string.Empty;
    [ObservableProperty]
    private string _canonicalUnit = string.Empty;
    [ObservableProperty]
    private string _matchMethod = string.Empty;
    [ObservableProperty]
    private double _matchScore;
    [ObservableProperty]
    private MatchConfidence _confidence;
    [ObservableProperty]
    private string _originalLine = string.Empty;
    [ObservableProperty]
    private string _quantityDisplay = string.Empty;
    [ObservableProperty]
    private string _caloriesText = string.Empty;
    [ObservableProperty]
    private string _proteinText = string.Empty;
    [ObservableProperty]
    private string _carbsText = string.Empty;
    [ObservableProperty]
    private string _fatText = string.Empty;

    [ObservableProperty]
    private bool _isTopCalories;
    [ObservableProperty]
    private bool _isTopProtein;
    [ObservableProperty]
    private bool _isTopCarbs;
    [ObservableProperty]
    private bool _isTopFat;

    [ObservableProperty]
    private double _weightInGrams;

    /// <summary>True when this ingredient maps to a produce dataset entry and seasonality is enabled.</summary>
    [ObservableProperty]
    private bool _hasSeasonality;

    /// <summary>Short hint such as "In season · +12 pts" shown next to the ingredient.</summary>
    [ObservableProperty]
    private string _seasonalityHint = string.Empty;

    /// <summary>State colour for the seasonality hint (green/orange/red).</summary>
    [ObservableProperty]
    private string _seasonalityColor = "Gray";

    /// <summary>Multi-line tooltip detail for this ingredient's seasonality.</summary>
    [ObservableProperty]
    private string _seasonalityTooltip = string.Empty;

    public Food? MatchedFood { get; set; }

    public bool ShowOriginalLine => IsMatched && !string.Equals(
        OriginalLine, FoodName, StringComparison.OrdinalIgnoreCase);

    public bool ShowReviewIndicator => !IsMatched || !string.Equals(
        MatchMethod, "Exact", StringComparison.OrdinalIgnoreCase);

    public string MatchQualityLabel => IsMatched && !string.Equals(MatchMethod, "Exact", StringComparison.OrdinalIgnoreCase)
        ? $"{MatchMethod} {MatchScore * 100:0}%"
        : string.Empty;

    public string DisplayText => IsMatched
        ? $"{QuantityDisplay} {FoodName}"
        : FoodName;

    /// <summary>Combined row tooltip: the original line plus seasonality detail when available.</summary>
    public string TooltipText => string.Join("\n", new[] { OriginalLine, SeasonalityTooltip }.Where(s => !string.IsNullOrEmpty(s)));

    /// <summary>
    /// Applies this ingredient's seasonality detail for the scored month. <paramref name="impactPoints"/>
    /// is how many points this ingredient contributes to (positive) or drags down (negative) the
    /// overall seasonality score; null when the ingredient isn't part of the scored set.
    /// </summary>
    public void ApplySeasonality(IngredientSeasonalityDetail? detail, double? impactPoints)
    {
        if (detail is null)
        {
            HasSeasonality = false;
            SeasonalityHint = string.Empty;
            SeasonalityColor = "Gray";
            SeasonalityTooltip = string.Empty;
            return;
        }

        string stateLabel = detail.State switch
        {
            SeasonalityState.InSeason => "In season",
            SeasonalityState.PartiallyInSeason => "Partially in season",
            _ => "Out of season",
        };
        string impact = impactPoints is double pts
            ? $"{stateLabel} · {pts:+0;-0} pts"
            : stateLabel;

        HasSeasonality = true;
        SeasonalityHint = impact;
        SeasonalityColor = detail.State switch
        {
            SeasonalityState.InSeason => "#2ECC71",
            SeasonalityState.PartiallyInSeason => "#F39C12",
            _ => "#E74C3C",
        };
        SeasonalityTooltip =
            $"Matched as: {detail.Name}\n" +
            $"Original: {OriginalLine}\n" +
            $"{stateLabel} this month\n" +
            $"Available ~{detail.EffectiveMonths:0.#} months/year\n" +
            $"Counts {detail.ScarcityWeight:0.0}x in the score\n" +
            (impactPoints is double p
                ? p >= 0
                    ? $"Contributes +{p:0} pts to the seasonality score"
                    : $"Costs {p:0} pts (out of season)"
                : string.Empty);
    }

    public static ParsedIngredientRow FromMatch(ParsedIngredientMatch match)
    {
        return new ParsedIngredientRow
        {
            IsMatched = match.IsMatched,
            FoodName = match.IsMatched
                ? match.MatchedFood!.Name
                : match.ParsedIngredient.FoodDescription,
            FoodDescription = match.ParsedIngredient.FoodDescription,
            CanonicalUnit = match.ParsedIngredient.CanonicalUnit,
            MatchMethod = match.MatchMethod,
            MatchScore = match.MatchScore,
            Confidence = match.Confidence,
            OriginalLine = match.ParsedIngredient.RawLine,
            MatchedFood = match.MatchedFood,
            QuantityDisplay = match.GetQuantityDisplay(),
            WeightInGrams = match.GetWeightInGrams(),
            CaloriesText = match.IsMatched ? $"{match.GetCalories():0} kcal" : "—",
            ProteinText = match.IsMatched ? $"P:{match.GetProtein():0.0}g" : "—",
            CarbsText = match.IsMatched ? $"C:{match.GetCarbohydrates():0.0}g" : "—",
            FatText = match.IsMatched ? $"F:{match.GetFat():0.0}g" : "—",
        };
    }
}
