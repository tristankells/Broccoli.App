using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Broccoli.Avalonia.Shared;
using Broccoli.Avalonia.Slices.Groceries;
using Broccoli.Avalonia.Slices.Planning;
using Broccoli.Avalonia.Slices.Recipes;
using Broccoli.Avalonia.Slices.Settings;

namespace Broccoli.Avalonia;

/// <summary>
/// Given a view model, returns the corresponding view. Uses an explicit type map rather than
/// reflection/naming-convention guessing, so it stays correct regardless of namespace/folder
/// layout and is safe under trimming/AOT (e.g. for future iOS targets).
/// </summary>
public class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> Factories = new()
    {
        [typeof(RecipesListViewModel)] = () => new RecipesListView(),
        [typeof(RecipeListPageViewModel)] = () => new RecipeListPageView(),
        [typeof(RecipeDetailViewModel)] = () => new RecipeDetailView(),
        [typeof(RecipeEditViewModel)] = () => new RecipeEditView(),
        [typeof(PlanningViewModel)] = () => new PlanningView(),
        [typeof(GroceriesViewModel)] = () => new GroceriesView(),
        [typeof(SettingsViewModel)] = () => new SettingsView(),
    };

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewModelType = param.GetType();
        if (Factories.TryGetValue(viewModelType, out var factory))
        {
            return factory();
        }

        return new TextBlock { Text = "Not Found: " + viewModelType.FullName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}