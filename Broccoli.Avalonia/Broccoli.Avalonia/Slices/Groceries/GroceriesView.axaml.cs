using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Broccoli.Avalonia.Slices.Groceries;

public partial class GroceriesView : UserControl
{
    public GroceriesView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GroceriesViewModel viewModel)
        {
            viewModel.SetClipboardTextAsync = async (string text) =>
            {
                TopLevel? topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is not null)
                {
                    await topLevel.Clipboard!.SetTextAsync(text);
                }
            };
        }
    }

    private void OnNewItemKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is GroceriesViewModel vm)
        {
            vm.AddItemCommand.Execute(null);
        }
    }
}
