using System.Collections.ObjectModel;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Shared;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Broccoli.Avalonia.Slices.Planning;

public partial class DayPlanViewModel : ViewModelBase
{
    private readonly IDailyFoodPlanService _planService;

    [ObservableProperty]
    private DailyFoodPlan? _selectedPlan;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private string? _errorMessage;

    public DayPlanViewModel()
        : this(new DailyFoodPlanService())
    {
    }

    public DayPlanViewModel(IDailyFoodPlanService planService)
    {
        _planService = planService;
        Load();
    }

    public ObservableCollection<DailyFoodPlan> Plans { get; } = new();

    public bool IsListVisible => SelectedPlan is null;

    public bool IsEditorVisible => SelectedPlan is not null;

    private void Load()
    {
        ErrorMessage = null;
        try
        {
            List<DailyFoodPlan> plans = _planService.GetAll();
            Plans.Clear();
            foreach (DailyFoodPlan p in plans)
            {
                Plans.Add(p);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NewPlan()
    {
        try
        {
            var plan = new DailyFoodPlan { Name = "New Plan" };
            plan.Tabs.Add(new DailyFoodPlanTab { Name = "Day 1" });
            plan = _planService.Add(plan);
            Plans.Add(plan);
            SelectedPlan = plan;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenPlan(DailyFoodPlan plan)
    {
        SelectedPlan = plan;
        RefreshVisibility();
    }

    [RelayCommand]
    private void BackToList()
    {
        SelectedPlan = null;
        RefreshVisibility();
        Load();
    }

    partial void OnSelectedPlanChanged(DailyFoodPlan? value) => RefreshVisibility();

    private void RefreshVisibility()
    {
        OnPropertyChanged(nameof(IsListVisible));
        OnPropertyChanged(nameof(IsEditorVisible));
    }

    [RelayCommand]
    private void DeletePlan(DailyFoodPlan plan)
    {
        try
        {
            _planService.Delete(plan.Id);
            Plans.Remove(plan);
            if (SelectedPlan?.Id == plan.Id)
            {
                SelectedPlan = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SavePlan()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        try
        {
            _planService.Update(SelectedPlan);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to save: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddTab()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        SelectedPlan.Tabs.Add(new DailyFoodPlanTab { Name = $"Day {SelectedPlan.Tabs.Count + 1}" });
    }

    [RelayCommand]
    private void DeleteTab(DailyFoodPlanTab tab)
    {
        if (SelectedPlan is null)
        {
            return;
        }

        SelectedPlan.Tabs.Remove(tab);
    }

    [RelayCommand]
    private void AddFoodRow()
    {
        if (SelectedPlan is null || SelectedPlan.Tabs.Count == 0)
        {
            return;
        }

        SelectedPlan.Tabs[0].Rows.Add(new DailyFoodPlanRow());
    }

    [RelayCommand]
    private void AddHeader()
    {
        if (SelectedPlan is null || SelectedPlan.Tabs.Count == 0)
        {
            return;
        }

        SelectedPlan.Tabs[0].Rows.Add(new DailyFoodPlanRow
        {
            RowType = DailyFoodPlanRowType.Header,
            HeaderName = "New Section",
        });
    }

    [RelayCommand]
    private void DeleteRow(DailyFoodPlanRow row)
    {
        if (SelectedPlan is null)
        {
            return;
        }

        foreach (DailyFoodPlanTab tab in SelectedPlan.Tabs)
        {
            tab.Rows.Remove(row);
        }
    }

    [RelayCommand]
    private void MoveRowUp(DailyFoodPlanRow row)
    {
        if (SelectedPlan is null)
        {
            return;
        }

        foreach (DailyFoodPlanTab tab in SelectedPlan.Tabs)
        {
            int idx = tab.Rows.IndexOf(row);
            if (idx > 0)
            {
                tab.Rows.RemoveAt(idx);
                tab.Rows.Insert(idx - 1, row);
            }
        }
    }

    [RelayCommand]
    private void MoveRowDown(DailyFoodPlanRow row)
    {
        if (SelectedPlan is null)
        {
            return;
        }

        foreach (DailyFoodPlanTab tab in SelectedPlan.Tabs)
        {
            int idx = tab.Rows.IndexOf(row);
            if (idx >= 0 && idx < tab.Rows.Count - 1)
            {
                tab.Rows.RemoveAt(idx);
                tab.Rows.Insert(idx + 1, row);
            }
        }
    }
}
