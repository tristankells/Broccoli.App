using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeListPageView : UserControl
{
    public RecipeListPageView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Staggered entrance for recipe cards. Only plays for the very first population of the page
    /// (<see cref="RecipeListPageViewModel.EntranceAnimationPending"/>); cards start at
    /// <c>Opacity="0"</c> offset down 20px (set in the template) and fade/slide in with a small
    /// per-index delay. Recycled containers keep <c>Opacity="1"</c> and are skipped.
    /// </summary>
    private void OnCardLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Border border
            || border.DataContext is not RecipeCardViewModel card
            || border.Opacity > 0
            || border.FindAncestorOfType<ItemsRepeater>() is not { } repeater
            || repeater.DataContext is not RecipeListPageViewModel viewModel
            || !viewModel.EntranceAnimationPending)
        {
            return;
        }

        int index = viewModel.FilteredRecipes.IndexOf(card);
        var delay = TimeSpan.FromMilliseconds(Math.Min(index, 12) * 40);

        if (border.Transitions is { } transitions)
        {
            foreach (ITransition transition in transitions)
            {
                if (transition is DoubleTransition doubleTransition)
                {
                    doubleTransition.Delay = delay;
                }
            }
        }

        border.Opacity = 1;
        if (border.RenderTransform is TranslateTransform translate)
        {
            translate.Y = 0;
        }
    }

    private void OnCardContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not RecipeCardViewModel card)
        {
            return;
        }

        e.Handled = true;

        var content = new StackPanel();

        var editItem = new Button
        {
            Content = "Edit",
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
        };
        editItem.Click += (_, _) => card.EditRequested?.Invoke(card);
        content.Children.Add(editItem);

        var menuItem = new Button
        {
            Content = "Add to shopping cart",
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
            HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Left,
        };
        menuItem.Click += (_, _) => card.AddToCartRequested?.Invoke(card);
        content.Children.Add(menuItem);

        var flyout = new Flyout { Content = content };
        flyout.ShowAt(control, true);
    }
}
