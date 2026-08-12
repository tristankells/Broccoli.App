using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Broccoli.Avalonia.Models;

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

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control source
            || source.DataContext is not GroceryListItem item)
        {
            return;
        }

        if (source is CheckBox)
        {
            return;
        }

        GroceriesViewModel? viewModel = FindViewModel();
        if (viewModel is not null)
        {
            viewModel.StartEditCommand.Execute(item);
        }
    }

    private void OnEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is Control control && control.DataContext is GroceryListItem item)
        {
            GroceriesViewModel? viewModel = FindViewModel();
            if (viewModel is not null)
            {
                viewModel.CommitEditCommand.Execute(item);
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Escape && sender is Control control2 && control2.DataContext is GroceryListItem item2)
        {
            item2.IsEditing = false;
            e.Handled = true;
        }
    }

    private void OnEditLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is Control control && control.DataContext is GroceryListItem item)
        {
            GroceriesViewModel? viewModel = FindViewModel();
            if (viewModel is not null)
            {
                viewModel.CommitEditCommand.Execute(item);
            }
        }
    }

    private GroceriesViewModel? FindViewModel()
    {
        Control? current = this;
        while (current is not null)
        {
            if (current.DataContext is GroceriesViewModel vm)
            {
                return vm;
            }

            current = current.Parent as Control;
        }

        return DataContext as GroceriesViewModel;
    }

    private void OnNewItemKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is GroceriesViewModel vm)
        {
            vm.AddItemCommand.Execute(null);
        }
    }
}
