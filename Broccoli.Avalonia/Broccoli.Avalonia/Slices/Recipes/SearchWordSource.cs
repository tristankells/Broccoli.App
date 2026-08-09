namespace Broccoli.Avalonia.Slices.Recipes;

[Flags]
internal enum SearchWordSource
{
    None = 0,
    Title = 1,
    Tags = 2,
    Ingredients = 4,
}
