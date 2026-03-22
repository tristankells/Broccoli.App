using Broccoli.Data.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Broccoli.App.Shared.Slices.MealPrep;

public partial class MealPrepPlans
{
    private List<MealPrepPlan> _plans      = new();
    private List<Recipe>       _allRecipes = new();
    private List<PantryItem>   _pantryItems = new();
    private bool               _isLoading  = true;

    // Which plans are currently expanded (all expanded by default)
    private HashSet<string> _expandedPlanIds = new();

    // Drag-and-drop state
    private string? _draggingId = null;
    private string? _dragOverId = null;

    // Inline rename
    private string?          _editingPlanId  = null;
    private string           _editingName    = string.Empty;
    private ElementReference _renameInputRef;
    private bool             _pendingFocus   = false;

    // AddRecipesToPlanDialog state
    private bool          _showAddRecipesDialog = false;
    private MealPrepPlan? _targetPlan           = null;

    // Cart dialog state
    private bool   _showCartDialog       = false;
    private string _combinedIngredients  = string.Empty;
    private string _combinedPlanName     = string.Empty;

    // -- Lifecycle ------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_pendingFocus)
        {
            _pendingFocus = false;
            try { await _renameInputRef.FocusAsync(); }
            catch { /* element may not be mounted yet */ }
        }
    }

    // -- Data loading ---------------------------------------------------------

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        StateHasChanged();

        try
        {
            var userId = AuthStateService.CurrentUserId!;

            _plans       = await MealPrepPlanService.GetAllAsync();
            _allRecipes  = (await RecipeService.GetAllAsync()).ToList();
            _pantryItems = await PantryService.GetAllAsync(userId);

            // Expand all plans by default
            _expandedPlanIds = _plans.Select(p => p.Id).ToHashSet();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading meal prep data: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    // -- Plan CRUD -------------------------------------------------------------

    private async Task CreateNewPlan()
    {
        try
        {
            var created = await MealPrepPlanService.AddAsync(new MealPrepPlan { Name = "New Plan" });
            _plans.Insert(0, created);
            _expandedPlanIds.Add(created.Id);
            StartRename(created);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating plan: {ex.Message}");
        }
    }

    private async Task DeletePlan(MealPrepPlan plan)
    {
        var confirmed = await JSRuntime.InvokeAsync<bool>(
            "confirm", $"Delete plan \"{plan.Name}\"? This cannot be undone.");

        if (!confirmed) return;

        try
        {
            await MealPrepPlanService.DeleteAsync(plan.Id);
            _plans.Remove(plan);
            _expandedPlanIds.Remove(plan.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting plan: {ex.Message}");
        }
    }

    // -- Expand / collapse -----------------------------------------------------

    private void ToggleExpand(string planId)
    {
        // Don't collapse while the user is renaming this plan
        if (_editingPlanId == planId) return;

        if (!_expandedPlanIds.Remove(planId))
            _expandedPlanIds.Add(planId);
    }

    // -- Drag-and-drop reorder -------------------------------------------------

    private void OnDragStart(string planId)
    {
        _draggingId = planId;
    }

    private void OnDragEnd()
    {
        _draggingId = null;
        _dragOverId = null;
    }

    private void OnDragOver(string planId)
    {
        // Only update (and trigger a re-render) when the hovered target changes.
        if (_dragOverId != planId)
            _dragOverId = planId;
    }

    private void OnDragLeave()
    {
        _dragOverId = null;
    }

    private async Task OnDrop(string targetPlanId)
    {
        _dragOverId = null;

        if (_draggingId is null || _draggingId == targetPlanId)
        {
            _draggingId = null;
            return;
        }

        var fromIndex = _plans.FindIndex(p => p.Id == _draggingId);
        var toIndex   = _plans.FindIndex(p => p.Id == targetPlanId);

        _draggingId = null;

        if (fromIndex < 0 || toIndex < 0) return;

        // Reorder in memory.
        // Insert at toIndex (pre-removal) so dragging down places the card after the
        // target, and dragging up places it before — the natural "swap" feel.
        var dragged = _plans[fromIndex];
        _plans.RemoveAt(fromIndex);
        _plans.Insert(toIndex, dragged);

        // Persist the new order to CosmosDB.
        try
        {
            await MealPrepPlanService.ReorderAsync(_plans.Select(p => p.Id).ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error persisting plan order: {ex.Message}");
        }
    }

    // -- Inline rename ---------------------------------------------------------

    private void StartRename(MealPrepPlan plan)
    {
        _editingPlanId = plan.Id;
        _editingName   = plan.Name;
        _expandedPlanIds.Add(plan.Id);
        _pendingFocus  = true;
    }

    private async Task SaveRename()
    {
        if (_editingPlanId is null) return;

        var plan = _plans.FirstOrDefault(p => p.Id == _editingPlanId);
        _editingPlanId = null;

        if (plan is null) return;

        var trimmed = _editingName.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed) && trimmed != plan.Name)
        {
            plan.Name = trimmed;
            try
            {
                await MealPrepPlanService.UpdateAsync(plan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error renaming plan: {ex.Message}");
            }
        }
    }

    private async Task OnRenameKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")  await SaveRename();
        if (e.Key == "Escape") _editingPlanId = null;
    }

    // -- Add Recipes dialog ----------------------------------------------------

    private void OpenAddRecipesDialog(MealPrepPlan plan)
    {
        _targetPlan           = plan;
        _showAddRecipesDialog = true;
    }

    private void HandleAddRecipesCancel()
    {
        _showAddRecipesDialog = false;
        _targetPlan           = null;
    }

    private async Task HandleAddRecipesConfirm(List<string> selectedIds)
    {
        _showAddRecipesDialog = false;
        if (_targetPlan is null) return;

        _targetPlan.RecipeIds = selectedIds;

        try
        {
            await MealPrepPlanService.UpdateAsync(_targetPlan);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating plan recipes: {ex.Message}");
        }

        _targetPlan = null;
    }

    // -- Cart dialog -----------------------------------------------------------

    private void OpenCartDialog(MealPrepPlan plan)
    {
        // Combine ingredient text from every recipe in the plan
        _combinedIngredients = string.Join("\n",
            plan.RecipeIds
                .Select(id => _allRecipes.FirstOrDefault(r => r.Id == id))
                .Where(r => r is not null)
                .Select(r => r!.Ingredients)
                .Where(i => !string.IsNullOrWhiteSpace(i)));

        _combinedPlanName = plan.Name;
        _showCartDialog   = true;
    }

    private void HandleCartCancel()
    {
        _showCartDialog = false;
    }

    private async Task HandleCartConfirm(List<string> selectedLines)
    {
        _showCartDialog = false;
        if (!selectedLines.Any()) return;

        try
        {
            var userId = AuthStateService.CurrentUserId!;
            await IngredientCartService.AddToCartAsync(selectedLines, userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding ingredients to grocery list: {ex.Message}");
        }
    }
}


