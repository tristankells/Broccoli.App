using Avalonia.Controls;

namespace Broccoli.Avalonia.Slices.Settings;

public partial class RecipeSettingsView : UserControl
{
    public RecipeSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RecipeSettingsViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.AvailableTargets))
                    PopulateTargets(vm);
            };
            PopulateTargets(vm);
        }
    }

    private void PopulateTargets(RecipeSettingsViewModel vm)
    {
        TargetComboBox.Items.Clear();
        foreach (var t in vm.AvailableTargets)
            TargetComboBox.Items.Add(new ComboBoxItem { Content = t.Name });
    }
}
