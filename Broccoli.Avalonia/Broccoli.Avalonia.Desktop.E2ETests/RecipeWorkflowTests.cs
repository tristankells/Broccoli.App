using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace Broccoli.Avalonia.Desktop.E2ETests;

/// <summary>
/// End-to-end test against the real desktop app, driven through Appium + the Windows driver
/// (WinAppDriver). Each test launches the app against a throwaway data folder, so real user
/// data is never touched, and the folder is wiped (fresh) before every launch.
/// </summary>
[TestClass]
public class RecipeWorkflowTests
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromMinutes(2);

    private string _appDataFolder = string.Empty;
    private AppiumSession? _session;

    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void Setup()
    {
        _appDataFolder = TestData.CreateScratchDataFolder();
        _session = AppiumSession.Launch(TestContext!, DesktopAppPaths.DesktopExe, _appDataFolder, CommandTimeout);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _session?.Dispose();
        TestData.DeleteScratchDataFolder(_appDataFolder);
    }

    [TestMethod]
    public void AddRecipe_SavesAllFieldsAndAppearsInRecipeList()
    {
        // Mock data for the recipe the test creates. Multiline fields use '\n' as the canonical
        // newline; WinAppDriver's SendKeys needs Enter key codes to insert real line breaks.
        string recipeName = "E2E " + Guid.NewGuid().ToString("N")[..10];
        const string servings = "4";
        const string prepTime = "15";
        const string cookTime = "45";
        const string ingredients = "2 cups flour\n1 cup sugar\n100g butter";
        const string directions = "Preheat the oven.\nMix everything together.\nBake until golden.";
        const string notes = "Great with a scoop of ice cream.";
        const string tags = "Dessert, Quick";
        const string source = "The Family Cookbook";
        const string url = "https://example.com/mock-recipe";

        // The Recipes page is the initial page; wait until the app has started and shows it.
        AppiumElement addRecipe = _session!.WaitForElement(By.XPath("//*[@AutomationId='addRecipeButton']"));
        addRecipe.Click();

        // Fill in the whole edit form.
        AppiumElement nameInput = _session.WaitForElement(By.XPath("//*[@AutomationId='recipeName']"));
        nameInput.SendKeys(recipeName);

        _session.WaitForElement(By.XPath("//*[@AutomationId='servingsInput']")).SendKeys(servings);
        _session.WaitForElement(By.XPath("//*[@AutomationId='prepTimeInput']")).SendKeys(prepTime);
        _session.WaitForElement(By.XPath("//*[@AutomationId='cookTimeInput']")).SendKeys(cookTime);

        _session.WaitForElement(By.XPath("//*[@AutomationId='ingredientsInput']"))
            .SendKeys(ToSendKeys(ingredients));
        _session.WaitForElement(By.XPath("//*[@AutomationId='directionsInput']"))
            .SendKeys(ToSendKeys(directions));
        _session.WaitForElement(By.XPath("//*[@AutomationId='notesInput']"))
            .SendKeys(ToSendKeys(notes));
        _session.WaitForElement(By.XPath("//*[@AutomationId='tagsInput']")).SendKeys(tags);
        _session.WaitForElement(By.XPath("//*[@AutomationId='sourceInput']")).SendKeys(source);
        _session.WaitForElement(By.XPath("//*[@AutomationId='urlInput']")).SendKeys(url);

        // Save and return to the list.
        AppiumElement save = _session.WaitForElement(By.XPath("//*[@AutomationId='saveRecipeButton']"));
        save.Click();

        // The saved recipe card should be visible on the list page.
        AppiumElement card = _session.WaitForElement(
            By.XPath($"//*[@AutomationId='recipeCard' and @Name='{recipeName}']"));
        Assert.IsNotNull(card, "The saved recipe card should be visible in the recipe list.");

        // Everything was persisted under the scratch folder (never the real app data).
        string recipesFolder = Path.Combine(_appDataFolder, "Recipes");
        Assert.IsTrue(Directory.Exists(recipesFolder), "A Recipes folder should exist under the scratch data folder.");
        string recipeFolder = Directory.GetDirectories(recipesFolder).Single();
        string markdown = File.ReadAllText(Path.Combine(recipeFolder, "recipe.md")).Replace("\r\n", "\n");

        Assert.IsTrue(markdown.Contains("name: " + recipeName), "Name should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("servings: " + servings), "Servings should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("prepTimeMinutes: " + prepTime), "Prep time should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("cookTimeMinutes: " + cookTime), "Cook time should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("source: " + source), "Source should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("url: " + url), "URL should be in the frontmatter.");
        Assert.IsTrue(markdown.Contains("- Dessert") && markdown.Contains("- Quick"), "Tags should be in the frontmatter.");

        Assert.IsTrue(markdown.Contains(ingredients), "Ingredients (including line breaks) should be saved in the body.");
        Assert.IsTrue(markdown.Contains(directions), "Directions (including line breaks) should be saved in the body.");
        Assert.IsTrue(markdown.Contains(notes), "Notes should be saved in the body.");
    }

    /// <summary>Converts '\n' into the Enter key code WinAppDriver needs to insert a line break.</summary>
    private static string ToSendKeys(string value) => value.Replace("\n", Keys.Enter);
}
