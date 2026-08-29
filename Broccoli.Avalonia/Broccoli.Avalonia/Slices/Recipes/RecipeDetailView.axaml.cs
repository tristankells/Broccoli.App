using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Broccoli.Avalonia.Slices.Recipes;

public partial class RecipeDetailView : UserControl
{
    public RecipeDetailView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fades an image thumbnail in from <c>Opacity="0"</c> once it is realized, so pictures
    /// don't pop in abruptly when the page appears.
    /// </summary>
    private void OnImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is Image image)
        {
            image.Opacity = 1;
        }
    }
}
