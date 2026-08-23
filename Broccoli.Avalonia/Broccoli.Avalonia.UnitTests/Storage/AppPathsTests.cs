using Broccoli.Avalonia.Storage;

namespace Broccoli.Avalonia.Tests.Storage;

[TestClass]
public class AppPathsTests
{
    [TestMethod]
    public void OverrideRootFolder_RedirectsDatabaseAndRecipesPaths()
    {
        string scratch = Path.Combine(
            Path.GetTempPath(), "broccoli-apppaths-test", Guid.NewGuid().ToString("N"));
        try
        {
            AppPaths.OverrideRootFolder(scratch);

            Assert.AreEqual(Path.Combine(scratch, "broccoli.db"), AppPaths.DatabaseFilePath);
            Assert.AreEqual(Path.Combine(scratch, "Recipes"), AppPaths.RecipesFolder);
            Assert.IsTrue(Directory.Exists(Path.Combine(scratch, "Recipes")));
        }
        finally
        {
            AppPaths.OverrideRootFolder(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Broccoli"));
            if (Directory.Exists(scratch))
            {
                Directory.Delete(scratch, recursive: true);
            }
        }
    }
}
