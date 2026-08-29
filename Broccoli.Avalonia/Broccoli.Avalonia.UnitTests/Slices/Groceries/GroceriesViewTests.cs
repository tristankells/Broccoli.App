using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Broccoli.Avalonia.Models;
using Broccoli.Avalonia.Slices.Groceries;
using Moq;

namespace Broccoli.Avalonia.Tests.Slices.Groceries;

[TestClass]
public class GroceriesViewTests
{
    [TestMethod]
    public void TypingThenPressingEnter_AddsItemToViewModel()
    {
        Mock<IGroceryListService> service = new();
        service.Setup(s => s.GetAll()).Returns(new List<GroceryListItem>());
        service.Setup(s => s.Add(It.IsAny<GroceryListItem>()))
            .Returns((GroceryListItem item) =>
            {
                item.Id = "test-1";
                return item;
            });

        var viewModel = new GroceriesViewModel(service.Object);

        HeadlessUiHost.Run(
            () => new GroceriesView { DataContext = viewModel },
            window =>
            {
                TextBox input = FindInput(window);
                input.Focus();
                window.KeyTextInput("Milk");
                window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
                Dispatcher.UIThread.RunJobs();

                Assert.AreEqual(1, viewModel.Items.Count, "Enter on the input should add an item.");
                Assert.AreEqual("Milk", viewModel.Items[0].Name);
                Assert.AreEqual(string.Empty, viewModel.NewItemText, "The input should clear after adding.");
                Assert.AreEqual("0 of 1 checked", viewModel.StatusText);
                Assert.AreEqual(string.Empty, input.Text, "The text box should reflect the cleared input.");
            });
    }

    [TestMethod]
    public void TypingEnablesAddButton_InitiallyDisabled()
    {
        Mock<IGroceryListService> service = new();
        service.Setup(s => s.GetAll()).Returns(new List<GroceryListItem>());

        var viewModel = new GroceriesViewModel(service.Object);

        HeadlessUiHost.Run(
            () => new GroceriesView { DataContext = viewModel },
            window =>
            {
                Button addButton = FindAddButton(window);
                Assert.IsFalse(addButton.IsEnabled, "Add should be disabled while the box is empty.");

                TextBox input = FindInput(window);
                input.Focus();
                window.KeyTextInput("Bread");
                Dispatcher.UIThread.RunJobs();

                Assert.IsTrue(addButton.IsEnabled, "Add should be enabled once text is entered.");
                Assert.AreEqual("Bread", viewModel.NewItemText, "Typing should flow two-way into the view model.");
            });
    }

    [TestMethod]
    public void ViewRendersExistingItems_ShowsRowsAndResetButton()
    {
        Mock<IGroceryListService> service = new();
        service.Setup(s => s.GetAll()).Returns(new List<GroceryListItem>
        {
            new() { Id = "a", Name = "Milk" },
            new() { Id = "b", Name = "Eggs" },
        });

        var viewModel = new GroceriesViewModel(service.Object);

        HeadlessUiHost.Run(
            () => new GroceriesView { DataContext = viewModel },
            window =>
            {
                Assert.AreEqual(1, HeadlessUiHost.FindVisualChildren<TextBlock>(window)
                    .Count(t => t.IsEffectivelyVisible && t.Text == "Milk"));
                Assert.AreEqual(1, HeadlessUiHost.FindVisualChildren<TextBlock>(window)
                    .Count(t => t.IsEffectivelyVisible && t.Text == "Eggs"));
                Assert.IsTrue(HeadlessUiHost.FindVisualChildren<Button>(window)
                    .Any(b => b.Content?.ToString() == "Reset List" && b.IsEffectivelyVisible),
                    "Reset List should show once the list has items.");
            });
    }

    [TestMethod]
    public void CheckingCheckbox_UpdatesItemAndAppliesCheckedClass()
    {
        Mock<IGroceryListService> service = new();
        var item = new GroceryListItem { Id = "a", Name = "Milk" };
        service.Setup(s => s.GetAll()).Returns(new List<GroceryListItem> { item });
        var viewModel = new GroceriesViewModel(service.Object);

        HeadlessUiHost.Run(
            () => new GroceriesView { DataContext = viewModel },
            window =>
            {
                CheckBox checkbox = HeadlessUiHost.FindVisualChildren<CheckBox>(window)[0];
                checkbox.IsChecked = true;
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();

                Assert.IsTrue(item.IsChecked, "Checking the box should persist to the item.");
                service.Verify(s => s.Update(It.IsAny<GroceryListItem>()), Times.Once);

                TextBlock nameBlock = HeadlessUiHost.FindVisualChildren<TextBlock>(window)
                    .First(t => t.Text == "Milk");
                Assert.IsTrue(nameBlock.Classes.Contains("checked"),
                    "Checked rows should get the strikethrough class.");
            });
    }

    private static TextBox FindInput(Window window) =>
        HeadlessUiHost.FindVisualChildren<TextBox>(window)
            .First(t => t.IsEffectivelyVisible);

    private static Button FindAddButton(Window window) =>
        HeadlessUiHost.FindVisualChildren<Button>(window)
            .First(b => b.Content?.ToString() == "Add");
}
