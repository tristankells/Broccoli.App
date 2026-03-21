using Broccoli.Data.Models;
using Broccoli.App.Shared.Services;
using Broccoli.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Broccoli.App.Shared.Pages;

public partial class DailyFoodPlanning
{
    // ── Injected services ─────────────────────────────────────────────────────

    [Inject] private IDailyFoodPlanService DailyFoodPlanService { get; set; } = default!;
    [Inject] private IRecipeService RecipeService { get; set; } = default!;
    [Inject] private IFoodService FoodService { get; set; } = default!;
    [Inject] private IMacroTargetService MacroTargetService { get; set; } = default!;
    [Inject] private IngredientParserService IngredientParserService { get; set; } = default!;
    [Inject] private IAuthenticationStateService AuthStateService { get; set; } = default!;

    // ── Data ──────────────────────────────────────────────────────────────────

    private List<DailyFoodPlan> _plans        = new();
    private List<Recipe>        _allRecipes   = new();
    private List<Food>          _allFoods     = new();
    private List<MacroTarget>   _macroTargets = new();

    // ── UI state ──────────────────────────────────────────────────────────────

    private bool _isLoading = true;

    /// <summary>The plan currently open in the editor. Null = plans-list view.</summary>
    private DailyFoodPlan? _selectedPlan;

    /// <summary>ID of the active tab in the editor.</summary>
    private string? _activeTabId;

    /// <summary>The active tab, derived from <see cref="_activeTabId"/>.</summary>
    private DailyFoodPlanTab? ActiveTab =>
        _selectedPlan?.Tabs.FirstOrDefault(t => t.Id == _activeTabId);

    /// <summary>The MacroTarget linked to the active tab, if any.</summary>
    private MacroTarget? ActiveTargetProfile =>
        ActiveTab?.MacroTargetId is { Length: > 0 } id
            ? _macroTargets.FirstOrDefault(m => m.Id == id)
            : null;

    // ── Tab rename state ──────────────────────────────────────────────────────

    private string? _renamingTabId;
    private string  _renamingTabName = string.Empty;
    private ElementReference _tabRenameInputRef;
    private bool _pendingTabRenameFocus;

    // ── Plan name editing ─────────────────────────────────────────────────────

    private bool   _editingPlanName;
    private string _editingPlanNameValue = string.Empty;
    private ElementReference _planNameInputRef;
    private bool _pendingPlanNameFocus;

    // ── Row drag-and-drop state ───────────────────────────────────────────────

    private string? _draggingRowId;
    private string? _dragOverRowId;

    // ── Debounced save ────────────────────────────────────────────────────────

    private readonly Dictionary<string, CancellationTokenSource> _debounceCts  = new();
    private readonly Dictionary<string, string>                  _planSaveStatus = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingTabRenameFocus)
        {
            _pendingTabRenameFocus = false;
            try { await _tabRenameInputRef.FocusAsync(); } catch { /* element may not be mounted */ }
        }
        if (_pendingPlanNameFocus)
        {
            _pendingPlanNameFocus = false;
            try { await _planNameInputRef.FocusAsync(); } catch { /* element may not be mounted */ }
        }
    }

    // ── Data loading ──────────────────────────────────────────────────────────

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            var userId = AuthStateService.CurrentUserId!;

            _plans        = await DailyFoodPlanService.GetAllAsync();
            _allRecipes   = (await RecipeService.GetAllAsync()).OrderBy(r => r.Name).ToList();
            _allFoods     = (await FoodService.GetAllAsync()).OrderBy(f => f.Name).ToList();
            _macroTargets = await MacroTargetService.GetAllAsync(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading daily food planning data: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // ── Plans-list actions ────────────────────────────────────────────────────

    private async Task CreateNewPlan()
    {
        try
        {
            var firstTab = new DailyFoodPlanTab
            {
                Id   = Guid.NewGuid().ToString(),
                Name = "Day 1"
            };
            var created = await DailyFoodPlanService.AddAsync(new DailyFoodPlan
            {
                Name = "New Plan",
                Tabs = new List<DailyFoodPlanTab> { firstTab }
            });
            _plans.Insert(0, created);
            OpenPlan(created);
            StartEditPlanName();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating plan: {ex.Message}");
        }
    }

    private async Task DeletePlan(DailyFoodPlan plan)
    {
        try
        {
            await DailyFoodPlanService.DeleteAsync(plan.Id);
            _plans.Remove(plan);
            if (_selectedPlan?.Id == plan.Id) BackToPlans();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting plan: {ex.Message}");
        }
    }

    // ── Navigation ────────────────────────────────────────────────────────────

    private void OpenPlan(DailyFoodPlan plan)
    {
        _selectedPlan = plan;
        _activeTabId  = plan.Tabs.FirstOrDefault()?.Id;
        _renamingTabId = null;
        _editingPlanName = false;
    }

    private void BackToPlans()
    {
        _selectedPlan    = null;
        _activeTabId     = null;
        _renamingTabId   = null;
        _editingPlanName = false;
    }

    // ── Plan name editing ─────────────────────────────────────────────────────

    private void StartEditPlanName()
    {
        if (_selectedPlan is null) return;
        _editingPlanNameValue = _selectedPlan.Name;
        _editingPlanName      = true;
        _pendingPlanNameFocus = true;
    }

    private async Task SavePlanName()
    {
        if (_selectedPlan is null || !_editingPlanName) return;
        _editingPlanName = false;
        var name = _editingPlanNameValue.Trim();
        if (string.IsNullOrEmpty(name)) name = "Unnamed Plan";
        _selectedPlan.Name = name;
        ScheduleSave(_selectedPlan);
        await Task.CompletedTask;
    }

    private async Task OnPlanNameKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")  await SavePlanName();
        if (e.Key == "Escape") { _editingPlanName = false; }
    }

    // ── Tab management ────────────────────────────────────────────────────────

    private void SelectTab(string tabId)
    {
        _activeTabId   = tabId;
        _renamingTabId = null;
    }

    private void AddTab()
    {
        if (_selectedPlan is null) return;

        var tab = new DailyFoodPlanTab
        {
            Id   = Guid.NewGuid().ToString(),
            Name = $"Day {_selectedPlan.Tabs.Count + 1}"
        };
        _selectedPlan.Tabs.Add(tab);
        _activeTabId = tab.Id;
        StartRenameTab(tab);
        ScheduleSave(_selectedPlan);
    }

    private void DeleteTab(DailyFoodPlanTab tab)
    {
        if (_selectedPlan is null) return;
        _selectedPlan.Tabs.Remove(tab);
        if (_activeTabId == tab.Id)
            _activeTabId = _selectedPlan.Tabs.FirstOrDefault()?.Id;
        ScheduleSave(_selectedPlan);
    }

    private void StartRenameTab(DailyFoodPlanTab tab)
    {
        _renamingTabId         = tab.Id;
        _renamingTabName       = tab.Name;
        _pendingTabRenameFocus = true;
    }

    private async Task SaveRenameTab()
    {
        if (_selectedPlan is null || _renamingTabId is null) return;

        var tab = _selectedPlan.Tabs.FirstOrDefault(t => t.Id == _renamingTabId);
        if (tab is not null)
        {
            tab.Name = string.IsNullOrWhiteSpace(_renamingTabName) ? "Tab" : _renamingTabName.Trim();
        }
        _renamingTabId = null;
        ScheduleSave(_selectedPlan);
        await Task.CompletedTask;
    }

    private async Task OnTabRenameKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")  await SaveRenameTab();
        if (e.Key == "Escape") { _renamingTabId = null; }
    }

    // ── Person (MacroTarget) selection ────────────────────────────────────────

    private void OnTabPersonChanged(ChangeEventArgs e)
    {
        if (ActiveTab is null || _selectedPlan is null) return;
        ActiveTab.MacroTargetId = e.Value?.ToString() is { Length: > 0 } v ? v : null;
        ScheduleSave(_selectedPlan);
    }

    // ── Row management ────────────────────────────────────────────────────────

    private void AddFoodRow()
    {
        if (ActiveTab is null || _selectedPlan is null) return;
        ActiveTab.Rows.Add(new DailyFoodPlanRow
        {
            Id       = Guid.NewGuid().ToString(),
            RowType  = DailyFoodPlanRowType.FoodEntry,
            Quantity = 1
        });
        ScheduleSave(_selectedPlan);
    }

    private void AddHeaderRow()
    {
        if (ActiveTab is null || _selectedPlan is null) return;
        ActiveTab.Rows.Add(new DailyFoodPlanRow
        {
            Id         = Guid.NewGuid().ToString(),
            RowType    = DailyFoodPlanRowType.Header,
            HeaderName = "Section"
        });
        ScheduleSave(_selectedPlan);
    }

    private void DeleteRow(DailyFoodPlanRow row)
    {
        if (ActiveTab is null || _selectedPlan is null) return;
        ActiveTab.Rows.Remove(row);
        ScheduleSave(_selectedPlan);
    }

    // ── Row field change handlers ─────────────────────────────────────────────

    private void OnHeaderNameChanged(DailyFoodPlanRow row, ChangeEventArgs e)
    {
        row.HeaderName = e.Value?.ToString() ?? string.Empty;
        if (_selectedPlan is not null) ScheduleSave(_selectedPlan);
    }

    private async Task OnFoodOrRecipeChanged(DailyFoodPlanRow row, ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(value))
        {
            row.FoodOrRecipeId = null;
            row.ServingName    = string.Empty;
            row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
        }
        else if (value.StartsWith("r:", StringComparison.Ordinal))
        {
            row.IsRecipe       = true;
            row.FoodOrRecipeId = value[2..];
            var recipe = _allRecipes.FirstOrDefault(r => r.Id == row.FoodOrRecipeId);
            row.ServingName = recipe is not null ? "serving" : string.Empty;
            await CalculateRowMacrosAsync(row);
        }
        else if (value.StartsWith("f:", StringComparison.Ordinal))
        {
            row.IsRecipe       = false;
            row.FoodOrRecipeId = value[2..];
            var food = _allFoods.FirstOrDefault(f => f.Id.ToString() == row.FoodOrRecipeId);
            row.ServingName = food?.Measure ?? string.Empty;
            await CalculateRowMacrosAsync(row);
        }

        if (_selectedPlan is not null) ScheduleSave(_selectedPlan);
    }

    private async Task OnServingChanged(DailyFoodPlanRow row, ChangeEventArgs e)
    {
        row.ServingName = e.Value?.ToString() ?? string.Empty;
        await CalculateRowMacrosAsync(row);
        if (_selectedPlan is not null) ScheduleSave(_selectedPlan);
    }

    private async Task OnQuantityChanged(DailyFoodPlanRow row, ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var qty) && qty > 0)
            row.Quantity = qty;
        await CalculateRowMacrosAsync(row);
        if (_selectedPlan is not null) ScheduleSave(_selectedPlan);
    }

    // ── Row drag-and-drop ─────────────────────────────────────────────────────

    private void OnRowDragStart(string rowId)
    {
        _draggingRowId = rowId;
    }

    private void OnRowDragEnd()
    {
        _draggingRowId = null;
        _dragOverRowId = null;
    }

    private void OnRowDragOver(string rowId)
    {
        if (_dragOverRowId != rowId)
            _dragOverRowId = rowId;
    }

    private void OnRowDragLeave()
    {
        _dragOverRowId = null;
    }

    private void OnRowDrop(string targetRowId, DailyFoodPlanTab tab)
    {
        _dragOverRowId = null;

        if (_draggingRowId is null || _draggingRowId == targetRowId)
        {
            _draggingRowId = null;
            return;
        }

        var rows      = tab.Rows;
        var fromIndex = rows.FindIndex(r => r.Id == _draggingRowId);
        var toIndex   = rows.FindIndex(r => r.Id == targetRowId);

        _draggingRowId = null;

        if (fromIndex < 0 || toIndex < 0) return;

        var dragged = rows[fromIndex];
        rows.RemoveAt(fromIndex);
        // When dragging downward the removal shifts indices by one, so re-find the target.
        toIndex = rows.FindIndex(r => r.Id == targetRowId);
        rows.Insert(toIndex, dragged);

        if (_selectedPlan is not null) ScheduleSave(_selectedPlan);
    }

    // ── Macro calculation ─────────────────────────────────────────────────────

    private async Task CalculateRowMacrosAsync(DailyFoodPlanRow row)
    {
        if (row.RowType == DailyFoodPlanRowType.Header || string.IsNullOrEmpty(row.FoodOrRecipeId))
        {
            row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
            return;
        }

        if (row.IsRecipe)
        {
            var recipe = _allRecipes.FirstOrDefault(r => r.Id == row.FoodOrRecipeId);
            if (recipe is null || string.IsNullOrWhiteSpace(recipe.Ingredients))
            {
                row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
                return;
            }

            if (recipe.Servings is null or <= 0)
            {
                // Cannot calculate per-serving macros without a serving count.
                row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
                return;
            }

            var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(recipe.Ingredients);
            double totalCals = 0, totalFat = 0, totalProt = 0, totalCarbs = 0;
            foreach (var m in matches.Where(m => m.IsMatched))
            {
                totalCals   += m.GetCalories();
                totalFat    += m.GetFat();
                totalProt   += m.GetProtein();
                totalCarbs  += m.GetCarbohydrates();
            }

            var servings = (double)recipe.Servings.Value;
            row.Calories  = totalCals   / servings * row.Quantity;
            row.Fat       = totalFat    / servings * row.Quantity;
            row.ProteinG  = totalProt   / servings * row.Quantity;
            row.CarbsG    = totalCarbs  / servings * row.Quantity;
        }
        else
        {
            var food = _allFoods.FirstOrDefault(f => f.Id.ToString() == row.FoodOrRecipeId);
            if (food is null)
            {
                row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
                return;
            }

            // Synthesise an ingredient string for the parser: "{qty} {unit} {food.Name}"
            var unit             = string.IsNullOrWhiteSpace(row.ServingName) ? food.Measure : row.ServingName;
            var ingredientString = $"{row.Quantity} {unit} {food.Name}";

            var matches = await IngredientParserService.ParseAndMatchIngredientsAsync(ingredientString);
            var match   = matches.FirstOrDefault(m => m.IsMatched);
            if (match is not null)
            {
                row.Calories = match.GetCalories();
                row.Fat      = match.GetFat();
                row.ProteinG = match.GetProtein();
                row.CarbsG   = match.GetCarbohydrates();
            }
            else
            {
                row.Calories = row.Fat = row.ProteinG = row.CarbsG = 0;
            }
        }
    }

    // ── Tab totals ────────────────────────────────────────────────────────────

    private static IEnumerable<DailyFoodPlanRow> FoodRows(DailyFoodPlanTab? tab) =>
        tab?.Rows.Where(r => r.RowType == DailyFoodPlanRowType.FoodEntry) ?? [];

    private double TotalCalories(DailyFoodPlanTab? tab) => FoodRows(tab).Sum(r => r.Calories);
    private double TotalFat     (DailyFoodPlanTab? tab) => FoodRows(tab).Sum(r => r.Fat);
    private double TotalProtein (DailyFoodPlanTab? tab) => FoodRows(tab).Sum(r => r.ProteinG);
    private double TotalCarbs   (DailyFoodPlanTab? tab) => FoodRows(tab).Sum(r => r.CarbsG);

    // ── Delta helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a CSS class for colour-coding a macro delta value.
    /// Green = within ±10% of target.
    /// Orange = 10–20% under target.
    /// Red = more than 20% under target OR over target by more than 10%.
    /// </summary>
    private static string GetDeltaClass(double delta, double target)
    {
        if (target <= 0) return string.Empty;
        var pct = delta / target;             // positive  = still under target
        return pct switch
        {
            > -0.1 and <= 0.1   => "delta-green",
            > 0.1 and <= 0.2    => "delta-orange",
            _                   => "delta-red"
        };
    }

    // ── Debounced save ────────────────────────────────────────────────────────

    private void ScheduleSave(DailyFoodPlan plan)
    {
        var planId = plan.Id;

        if (_debounceCts.TryGetValue(planId, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = new CancellationTokenSource();
        _debounceCts[planId] = cts;

        _planSaveStatus[planId] = "Saving…";
        StateHasChanged();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(2000, cts.Token);
                await DailyFoodPlanService.UpdateAsync(plan);
                await InvokeAsync(() =>
                {
                    _planSaveStatus[planId] = "Saved ✓";
                    StateHasChanged();
                });
            }
            catch (OperationCanceledException)
            {
                // A newer mutation arrived — this task is superseded.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving daily food plan {planId}: {ex.Message}");
                await InvokeAsync(() =>
                {
                    _planSaveStatus[planId] = "Save failed ✗";
                    StateHasChanged();
                });
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the composite "f:{id}" / "r:{id}" value used by the item &lt;select&gt;.
    /// </summary>
    private static string GetRowSelectValue(DailyFoodPlanRow row)
    {
        if (string.IsNullOrEmpty(row.FoodOrRecipeId)) return string.Empty;
        return row.IsRecipe ? $"r:{row.FoodOrRecipeId}" : $"f:{row.FoodOrRecipeId}";
    }

    /// <summary>
    /// Returns true when the row references a recipe that has no servings count,
    /// meaning the per-serving macro calculation is not possible.
    /// </summary>
    private bool RowHasMissingServings(DailyFoodPlanRow row)
    {
        if (!row.IsRecipe || string.IsNullOrEmpty(row.FoodOrRecipeId)) return false;
        var recipe = _allRecipes.FirstOrDefault(r => r.Id == row.FoodOrRecipeId);
        return recipe is not null && (recipe.Servings is null or <= 0);
    }

    /// <summary>Returns the display name for a given row's food/recipe selection.</summary>
    private string GetRowDisplayName(DailyFoodPlanRow row)
    {
        if (string.IsNullOrEmpty(row.FoodOrRecipeId)) return string.Empty;
        if (row.IsRecipe)
        {
            return _allRecipes.FirstOrDefault(r => r.Id == row.FoodOrRecipeId)?.Name ?? string.Empty;
        }
        return _allFoods.FirstOrDefault(f => f.Id.ToString() == row.FoodOrRecipeId)?.Name ?? string.Empty;
    }

    private string GetSaveStatus(string planId) =>
        _planSaveStatus.TryGetValue(planId, out var s) ? s : string.Empty;
}



