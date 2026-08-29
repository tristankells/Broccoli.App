using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class RecipeSettingsViewModel : ViewModelBase
{
    private readonly IMacroTargetService _macroService;

    [ObservableProperty]
    private bool _comparisonEnabled;
    [ObservableProperty]
    private string _comparisonPersonId = string.Empty;
    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _showCardImage = true;
    [ObservableProperty]
    private bool _showCardTags = true;
    [ObservableProperty]
    private bool _showCardSeasonality = true;
    [ObservableProperty]
    private bool _showCardNutrition = true;

    [ObservableProperty]
    private bool _showCardCalorieMatch;
    [ObservableProperty]
    private double _calorieMatchTolerancePercent = 15;
    [ObservableProperty]
    private int _historyBackupCount = 10;
    [ObservableProperty]
    private AutoBalanceStrategy _autoBalanceStrategy = AutoBalanceStrategy.IndependentSinglePass;

    /// <summary>All list-view columns with their visibility and order, for the settings UI.</summary>
    public ObservableCollection<RecipeListColumnOption> ListColumnOptions { get; } = new();

    public RecipeSettingsViewModel()
        : this(new MacroTargetService())
    {
    }

    public RecipeSettingsViewModel(IMacroTargetService macroService)
    {
        _macroService = macroService;
        Load();
        WeakReferenceMessenger.Default.Register<MacroTargetsChangedMessage>(this, (_, _) => Load());
    }

    public List<MacroTarget> AvailableTargets { get; } = new();

    public List<AutoBalanceStrategyOption> AutoBalanceStrategyOptions { get; } = new()
    {
        new("Independent single-pass", "Scale the leading contributor of each selected macro; simple, but other macros can drift.", AutoBalanceStrategy.IndependentSinglePass),
        new("Exact linear solve", "Solve a linear system over the leading contributors to hit all selected targets exactly.", AutoBalanceStrategy.LinearSolve),
    };

    public AutoBalanceStrategyOption SelectedAutoBalanceStrategyOption
    {
        get => AutoBalanceStrategyOptions.FirstOrDefault(o => o.Value == AutoBalanceStrategy) ?? AutoBalanceStrategyOptions[0];
        set
        {
            if (value is not null)
            {
                AutoBalanceStrategy = value.Value;
            }
        }
    }

    private const int EstimatedKilobytesPerBackup = 2;

    /// <summary>Approximate on-disk footprint of the current backup count, for the settings hint.</summary>
    public string BackupStorageHintText =>
        $"Each backup is roughly {EstimatedKilobytesPerBackup} KB. Keeping {HistoryBackupCount} versions uses about {HistoryBackupCount * EstimatedKilobytesPerBackup} KB per recipe.";

    /// <summary>Reminds the user that the very first backup is always preserved.</summary>
    public string BackupCountHintText =>
        "The original (first) version is always kept; this limit applies to additional recent versions.";

    public int SelectedTargetIndex
    {
        get
        {
            int idx = AvailableTargets.FindIndex(t => t.Id == ComparisonPersonId);
            return idx >= 0 ? idx : 0;
        }

        set
        {
            if (value >= 0 && value < AvailableTargets.Count)
            {
                ComparisonPersonId = AvailableTargets[value].Id;
            }
        }
    }

    private void Load()
    {
        MacroTargetSettings settings = _macroService.GetSettings();
        ComparisonEnabled = settings.RecipeMealComparisonEnabled;
        ComparisonPersonId = settings.RecipeMealComparisonPersonId;
        ShowCardImage = settings.ShowCardImage;
        ShowCardTags = settings.ShowCardTags;
        ShowCardSeasonality = settings.ShowCardSeasonality;
        ShowCardNutrition = settings.ShowCardNutrition;
        ShowCardCalorieMatch = settings.ShowCardCalorieMatch;
        CalorieMatchTolerancePercent = settings.CalorieMatchTolerancePercent;
        HistoryBackupCount = settings.RecipeHistoryBackupCount;
        AutoBalanceStrategy = settings.AutoBalanceStrategy;
        LoadListColumnOptions(settings.RecipeListColumns);

        List<MacroTarget> targets = _macroService.GetAll();
        AvailableTargets.Clear();
        AvailableTargets.AddRange(targets);

        if (AvailableTargets.Count > 0 && string.IsNullOrWhiteSpace(ComparisonPersonId))
        {
            ComparisonPersonId = AvailableTargets[0].Id;
        }

        OnPropertyChanged(nameof(SelectedTargetIndex));
        OnPropertyChanged(nameof(SelectedAutoBalanceStrategyOption));
    }

    partial void OnHistoryBackupCountChanged(int value) => OnPropertyChanged(nameof(BackupStorageHintText));

    private void LoadListColumnOptions(string? serialized)
    {
        RecipeListColumn[] stored = RecipeListColumnDefinitions.Parse(serialized);
        List<RecipeListColumn> remaining = Enum.GetValues<RecipeListColumn>()
            .Except(stored)
            .ToList();

        ListColumnOptions.Clear();
        foreach (RecipeListColumn column in stored)
        {
            ListColumnOptions.Add(new RecipeListColumnOption(column, isSelected: true));
        }

        foreach (RecipeListColumn column in remaining)
        {
            ListColumnOptions.Add(new RecipeListColumnOption(column, isSelected: false));
        }

        UpdateMoveState();
    }

    private void UpdateMoveState()
    {
        for (int i = 0; i < ListColumnOptions.Count; i++)
        {
            ListColumnOptions[i].CanMoveUp = i > 0;
            ListColumnOptions[i].CanMoveDown = i < ListColumnOptions.Count - 1;
        }
    }

    [RelayCommand]
    private void MoveColumnUp(RecipeListColumnOption option)
    {
        int index = ListColumnOptions.IndexOf(option);
        if (index <= 0)
        {
            return;
        }

        ListColumnOptions.Move(index, index - 1);
        UpdateMoveState();
    }

    [RelayCommand]
    private void MoveColumnDown(RecipeListColumnOption option)
    {
        int index = ListColumnOptions.IndexOf(option);
        if (index < 0 || index >= ListColumnOptions.Count - 1)
        {
            return;
        }

        ListColumnOptions.Move(index, index + 1);
        UpdateMoveState();
    }

    [RelayCommand]
    private void Save()
    {
        StatusMessage = null;
        MacroTargetSettings settings = _macroService.GetSettings();
        settings.RecipeMealComparisonEnabled = ComparisonEnabled;
        settings.RecipeMealComparisonPersonId = ComparisonPersonId;
        settings.ShowCardImage = ShowCardImage;
        settings.ShowCardTags = ShowCardTags;
        settings.ShowCardSeasonality = ShowCardSeasonality;
        settings.ShowCardNutrition = ShowCardNutrition;
        settings.ShowCardCalorieMatch = ShowCardCalorieMatch;
        settings.CalorieMatchTolerancePercent = CalorieMatchTolerancePercent;
        settings.RecipeHistoryBackupCount = HistoryBackupCount;
        settings.AutoBalanceStrategy = AutoBalanceStrategy;
        settings.RecipeListColumns = RecipeListColumnDefinitions.Serialize(
            ListColumnOptions.Where(option => option.IsSelected).Select(option => option.Column));
        _macroService.SaveSettings(settings);
        StatusMessage = "Saved.";
        WeakReferenceMessenger.Default.Send(new CardSettingsChangedMessage());
    }
}

public sealed class AutoBalanceStrategyOption
{
    public AutoBalanceStrategyOption(string name, string description, AutoBalanceStrategy value)
    {
        Name = name;
        Description = description;
        Value = value;
    }

    public string Name { get; }

    public string Description { get; }

    public AutoBalanceStrategy Value { get; }
}
