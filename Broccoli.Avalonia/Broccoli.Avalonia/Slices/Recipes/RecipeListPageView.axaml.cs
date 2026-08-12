using Avalonia.Controls;
using Avalonia.Input;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeListPageView : UserControl
{
    public RecipeListPageView()
    {
        InitializeComponent();
    }

    private void OnCardContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (sender is not Control control || control.DataContext is not RecipeCardViewModel card)
        {
            return;
        }

        e.Handled = true;

        var content = new StackPanel();
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
