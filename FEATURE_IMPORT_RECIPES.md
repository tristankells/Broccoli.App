# Feature Plan: Recipe Import (Paprika HTML Export)

## Overview

A multi-step import dialog triggered from the Recipes page. Step 1 lets the user select an import format (starting with *Paprika — HTML Export*), shows format-specific export instructions, and accepts multiple drag-and-dropped HTML files. Step 2 shows a preview table of all parsed results — duplicates (matched by name) are excluded by default but can be individually opted in. Confirmed recipes are bulk-saved via the existing `IRecipeService.AddAsync`. Images are skipped; all HTML is stripped to plain text.

---

## Decisions Made

| Question | Decision |
|---|---|
| Ingredients/Directions text format | Strip all HTML tags, produce plain-text newline-separated strings matching the existing `Recipe` model |
| Images | Skipped on import — `Images` list left empty |
| Duplicate matching strategy | Case-insensitive `Name` match against existing recipes |
| Duplicate opt-in | Shown in preview with `⚠️ Duplicate` badge, checkbox **unchecked by default**, user must opt in per row |
| Bulk save error handling | Continue saving all selected recipes; show a per-recipe success/failure summary after the batch finishes |
| Future format extensibility | Build an `IImportFormat` abstraction from the start so new formats can be added without touching the dialog |

---

## Architecture

### New Files

```
Broccoli.App.Shared/
├── Models/
│   └── ImportRecipeResult.cs           # Parsed recipe + status wrapper
├── Services/
│   ├── IImportFormat.cs                # Abstraction for import formats
│   ├── PaprikaHtmlImportFormat.cs      # Paprika HTML implementation
│   └── RecipeImportService.cs          # Orchestrates parsing + duplicate detection
└── Components/
    ├── ImportRecipesDialog.razor        # Two-step modal UI
    ├── ImportRecipesDialog.razor.cs     # Code-behind
    └── ImportRecipesDialog.razor.css    # Styles
```

### Modified Files

```
Broccoli.App.Shared/
├── Broccoli.App.Shared.csproj         # Add AngleSharp NuGet package
└── Pages/
    ├── Recipes.razor                   # Add 📥 Import button + dialog component
    └── Recipes.razor.cs                # Add showImportDialog state + handlers
```

---

## Step-by-Step Implementation

### Step 1 — Add AngleSharp NuGet

Add to `Broccoli.App.Shared.csproj`:

```xml
<PackageReference Include="AngleSharp" Version="1.x" />
```

AngleSharp supports .NET WASM and MAUI environments and provides CSS selector querying for reliable parsing of the Paprika HTML structure.

---

### Step 2 — `ImportRecipeResult.cs`

Location: `Broccoli.App.Shared/Models/ImportRecipeResult.cs`

```csharp
public enum ImportStatus { ReadyToImport, Duplicate, ParseError }

public class ImportRecipeResult
{
    public string FileName { get; set; } = string.Empty;
    public Recipe? Recipe { get; set; }
    public ImportStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    // Controlled by the preview table checkboxes.
    // true for ReadyToImport, false for Duplicate and ParseError by default.
    public bool IsSelected { get; set; }
}
```

---

### Step 3 — `IImportFormat.cs`

Location: `Broccoli.App.Shared/Services/IImportFormat.cs`

Defines the contract for all future import format implementations:

```csharp
public interface IImportFormat
{
    // Display name shown in the format dropdown, e.g. "Paprika — HTML Export"
    string DisplayName { get; }

    // File extensions accepted by the file input, e.g. ".html"
    string FileExtension { get; }

    // Step-by-step instructions shown in the dialog for this format
    string ExportInstructions { get; }

    // Parses raw file content into a Recipe. Throws ImportParseException on failure.
    Task<Recipe> ParseAsync(string fileContent);
}
```

Adding a new import format in the future (e.g. Paprika YAML, Mealie JSON) only requires a new class implementing this interface — no dialog changes needed.

---

### Step 4 — `PaprikaHtmlImportFormat.cs`

Location: `Broccoli.App.Shared/Services/PaprikaHtmlImportFormat.cs`

Uses AngleSharp to query the Paprika HTML structure via `itemprop` attributes and CSS classes.

**Field mapping:**

| Recipe Field | HTML Selector | Notes |
|---|---|---|
| `Name` | `[itemprop="name"]` | Inner text |
| `Ingredients` | `[itemprop="recipeIngredient"]` | One `<p>` per ingredient; join with `\n`; strip `<strong>` tags |
| `Directions` | `[itemprop="recipeInstructions"] p` | Join with `\n\n`; strip all tags |
| `Notes` | `[itemprop="comment"] p` | Join with `\n`; strip all tags |
| `Tags` | `.categories` | Comma-split, trim whitespace |
| `Servings` | `.metadata` text after `"Servings:"` | Parse as `int` |
| `CookTimeMinutes` | `.metadata` text after `"Total Time:"` | Parse as `int` |
| `Source` | `[itemprop="author"]` | Inner text |
| `Images` | — | Skipped; left as empty list |

**Export instructions text** (shown in Step 1 of the dialog):
```
1. Open Paprika on your device.
2. Select the recipes you want to export (or select all).
3. Tap the Share / Export button.
4. Choose "HTML" as the export format.
5. Save or share the exported .html file(s) to your device.
6. Drop the file(s) into the box below.
```

---

### Step 5 — `RecipeImportService.cs`

Location: `Broccoli.App.Shared/Services/RecipeImportService.cs`

Orchestrates parsing and duplicate detection. Injected into the dialog.

```csharp
public class RecipeImportService
{
    // Parses fileContent using the given format.
    // Compares result name (case-insensitive) against existingRecipeNames.
    // Returns an ImportRecipeResult with Status and IsSelected pre-set.
    public async Task<ImportRecipeResult> ParseFileAsync(
        IImportFormat format,
        string fileName,
        string fileContent,
        IEnumerable<string> existingRecipeNames);

    // Convenience: processes a batch of (fileName, fileContent) pairs.
    public async Task<List<ImportRecipeResult>> ParseFilesAsync(
        IImportFormat format,
        IEnumerable<(string FileName, string Content)> files,
        IEnumerable<string> existingRecipeNames);
}
```

---

### Step 6 — `ImportRecipesDialog.razor`

Location: `Broccoli.App.Shared/Components/ImportRecipesDialog.razor`

A two-step modal following the same pattern as the existing `AddIngredientsDialog`.

#### Parameters

```csharp
[Parameter] public bool IsVisible { get; set; }
[Parameter] public IEnumerable<Recipe> ExistingRecipes { get; set; } = [];
[Parameter] public EventCallback OnCancel { get; set; }
[Parameter] public EventCallback<List<Recipe>> OnConfirm { get; set; }  // selected recipes only
```

#### Step 1 — Select Format & Upload

- **Format dropdown** — `<select>` bound to the active `IImportFormat`. Initially one option: *Paprika — HTML Export*. Changing format updates the instructions panel and resets any uploaded files.
- **Instructions panel** — collapsible `<details>` element showing `activeFormat.ExportInstructions` as a numbered list. Collapsed by default, open on first load.
- **Drop zone** — `<InputFile multiple accept=".html">` styled as a dashed drop zone with text *"Drag & drop .html files here, or click to browse"*. Shows a file count badge once files are chosen (e.g. *"3 files selected"*).
- **Next button** — disabled until at least one file is chosen. Clicking it triggers parsing (shows a spinner) and advances to Step 2.

#### Step 2 — Preview & Confirm

A table with the following columns:

| Column | Content |
|---|---|
| (checkbox) | Checked for `ReadyToImport`; unchecked for `Duplicate`; hidden for `ParseError` |
| File | `FileName` in muted text |
| Recipe Name | Bold name, or *"—"* on parse error |
| Tags | Tag chips (first 3, +N overflow) |
| Servings | Number or *"—"* |
| Status | Badge (see below) |

**Status badges:**

| Status | Badge | Extra detail |
|---|---|---|
| `ReadyToImport` | `✅ Ready` | — |
| `Duplicate` | `⚠️ Duplicate` | Muted note: *"A recipe with this name already exists. Check to import anyway."* |
| `ParseError` | `❌ Error` | Inline error message in red |

**Footer:**
- *"Import N recipes"* button — N is the live count of checked rows; disabled if N = 0.
- *"← Back"* button — returns to Step 1 without losing the file selection.
- After clicking Import: each row shows a saving spinner, then `✅ Saved` or `❌ Failed: [reason]` inline. Once all done, a *"Close"* button appears.

---

### Step 7 — Wire up `Recipes.razor` / `Recipes.razor.cs`

**`Recipes.razor`** — add Import button to the page header alongside the existing Add button:

```razor
<button class="btn btn-secondary import-button" @onclick="OpenImportDialog">
    <span class="icon">📥</span> Import
</button>
```

Add the dialog component below the existing `<AddIngredientsDialog>`:

```razor
<ImportRecipesDialog IsVisible="showImportDialog"
                     ExistingRecipes="allRecipes"
                     OnCancel="CloseImportDialog"
                     OnConfirm="HandleImportConfirm" />
```

**`Recipes.razor.cs`** — add state and handlers:

```csharp
private bool showImportDialog = false;

private void OpenImportDialog() => showImportDialog = true;
private void CloseImportDialog() => showImportDialog = false;

private async Task HandleImportConfirm(List<Recipe> recipesToImport)
{
    showImportDialog = false;
    await LoadRecipes(); // Refresh the recipe list after import
}
```

> `RecipeImportService` handles all `AddAsync` calls internally and reports per-recipe results back to the dialog before `OnConfirm` is invoked, so the confirm handler only needs to refresh the list.

---

## User Flow Summary

```
Recipes Page
  └─ [📥 Import] clicked
       └─ Step 1: Format & Upload
            ├─ Select format: "Paprika — HTML Export"
            ├─ Read export instructions (collapsible)
            ├─ Drop/select .html files
            └─ [Next →] (parses files, shows spinner)
                 └─ Step 2: Preview Table
                      ├─ ✅ Ready rows — pre-checked
                      ├─ ⚠️ Duplicate rows — unchecked (opt-in)
                      ├─ ❌ Error rows — no checkbox, shows reason
                      ├─ [← Back]
                      └─ [Import N Recipes]
                           └─ Per-row save progress → summary
                                └─ [Close] → Recipes list refreshed
```

---

## Resolved Questions

| Question | Answer |
|---|---|
| Instructions detail | Plain numbered text is sufficient for the MVP — no screenshots needed |
| Duplicate opt-in scope | Importing a duplicate creates a **second recipe** with the same name — no overwrite |


