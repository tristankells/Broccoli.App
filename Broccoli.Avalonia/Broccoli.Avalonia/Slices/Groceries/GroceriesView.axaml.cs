using Avalonia.Controls;
using Avalonia.Input;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class GroceriesView : UserControl
{
    public GroceriesView()
    {
        InitializeComponent();
    }

    private void OnNewItemKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is GroceriesViewModel vm)
        {
            vm.AddItemCommand.Execute(null);
        }
    }
}
