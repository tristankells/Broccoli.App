using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeListPageView : UserControl
{
    private bool _entranceClearScheduled;

    public RecipeListPageView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Cards start at <c>Opacity="0"</c> offset down 20px (set in the template) and are revealed
    /// once realized. On the very first population of the page they fade/slide in with a small
    /// per-index stagger; afterwards (the view is recreated by ViewLocator on every navigation)
    /// they are just shown immediately. Once the first batch has animated in, the pending flag is
    /// cleared so the stagger never replays. Recycled containers keep <c>Opacity="1"</c> and are
    /// skipped.
    /// </summary>
    private void OnCardLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Border border
            || border.DataContext is not RecipeCardViewModel card
            || border.Opacity > 0
            || border.FindAncestorOfType<ItemsRepeater>() is not { } repeater
            || repeater.DataContext is not RecipeListPageViewModel viewModel)
        {
            return;
        }

        if (viewModel.EntranceAnimationPending)
        {
            AnimateCardIn(border, card, viewModel);
        }
        else
        {
            // Entrance already played once (e.g. returning to the tab): reveal instantly. Clear the
            // entrance transitions so the Opacity/translate set below isn't animated.
            border.Transitions = null;
            border.Opacity = 1;
            if (border.RenderTransform is TranslateTransform translate)
            {
                translate.Y = 0;
            }
        }
    }

    private void AnimateCardIn(Border border, RecipeCardViewModel card, RecipeListPageViewModel viewModel)
    {
        // Cards of one batch realize across a single layout pass, so clear the pending flag a
        // moment after the longest possible stagger (12 * 40ms delay + 300ms duration) instead of
        // on the first card, which would cut the rest of the batch off.
        if (!_entranceClearScheduled)
        {
            _entranceClearScheduled = true;
            DispatcherTimer.RunOnce(
                () => viewModel.EntranceAnimationPending = false,
                TimeSpan.FromMilliseconds(1000));
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
