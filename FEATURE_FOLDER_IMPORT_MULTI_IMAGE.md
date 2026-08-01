# Paprika Folder Import + Multi-Image Recipes — Implementation Plan

## Overview

Two interrelated features built on a shared foundation:

1. **Multi-image recipes** — `Recipe.Images` becomes a true gallery with a `HeroImageIndex` to
   designate the primary photo. The detail page gains a carousel with per-image upload / remove /
   set-hero controls. Read-only view and list cards follow.

2. **Paprika folder import** — The import dialog gains a **Folder** mode that reads an entire
   Paprika export directory (`Recipes/*.html` + `Recipes/Images/{UUID}/*.jpg`), collects **all**
   images for each recipe via their UUID subfolder, uploads to Cloudinary using the pre-generated
   recipe ID, and streams live per-recipe progress during the save step.

---

## Architecture Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Image storage | Cloudinary via `IRecipeImageService.UploadAsync` | Existing service; keeps CosmosDB documents small |
| Recipe ID during import | Pre-generate before `AddAsync`; fix `AddAsync` to preserve it | Single Cosmos write per recipe; Cloudinary path is stable and predictable |
| Preview thumbnail | JS Canvas resize **client-side** before bytes reach C# | Prevents memory pressure on large batch imports (70+ recipes) |
| Folder file access | `webkitdirectory` on `InputFile` + JS `webkitRelativePath` | Web-only requirement; no MAUI path needed |
| Finding all images per recipe | Extract UUID from `[itemprop="image"]` src; collect all `Images/{UUID}/*.jpg` from the folder map | Reliable; no need to parse PhotoSwipe JS |
| Upload timing in import | Upload all images → then `AddAsync` (ID already set) | **Single Cosmos write** per recipe; no orphan documents on failure |
| MAUI support | Not in scope | Folder picker on MAUI is a separate feature |

---

## Export Folder Structure Reference

```
Export 2026-03-26 22.24.01/
  Recipes/
    Chicken Stew.html          ← one file per recipe
    Asian Chicken Poke Bowl with Slaw & Mayonnaise.html
    ...
    Images/
      A22628EB-0EC5-4A40-8ECB-00357B060637/    ← UUID maps to a recipe
        91115C0C-4085-41FF-9F5C-AEAFB3BAEB1D.jpg
        3B7FE121-...jpg
        ...                                      ← multiple images per recipe
    Resources/
      PhotoSwipe/   ← ignored
  index.html        ← ignored
```

The `[itemprop="image"]` `src` attribute in each HTML file contains the path to the **hero** image
AND encodes the UUID folder name: `Images/{UUID}/{imageGuid}.jpg`.  
All images for that recipe live in `Images/{UUID}/` — collected by prefix match, no JS parsing
needed.

---

## Data Flow

```
┌─ User picks folder ──────────────────────────────────────────────────────┐
│  HandleFolderChanged                                                      │
│    JS: folderImport.getRelativePaths("folder-input")  → string[] paths   │
│    Zip IBrowserFile[] + paths                                             │
│    Split: htmlFiles[] + imageMap{ "Images/UUID/img.jpg" → IBrowserFile } │
│    Show "62 recipes + 187 images found"                                   │
└───────────────────────────────────────────────────────────────────────────┘

┌─ Next → AdvanceToPreview ─────────────────────────────────────────────────┐
│  For each .html file:                                                      │
│    1. Read HTML content via IBrowserFile.OpenReadStream                    │
│    2. PaprikaHtmlImportFormat.ParseFolderFileAsync(html, paths, resolver) │
│         a. Parse recipe (AngleSharp, same logic as ParseAsync)            │
│         b. Extract hero src → UUID                                        │
│         c. Filter imageMap keys by "Images/{UUID}/" prefix               │
│         d. Hero path goes FIRST; remaining in directory order             │
│         e. For each path: resolver(path) → IBrowserFile → byte[]         │
│         f. Return (Recipe, List<PendingImageData>)                        │
│    3. JS: folderImport.generateThumbnail(heroBytes, 120, 90)             │
│         → small data: URI stored in ImportRecipeResult.ImageThumbnailDataUri│
│    4. Store PendingImageData[] in ImportRecipeResult.PendingImages        │
└───────────────────────────────────────────────────────────────────────────┘

┌─ Confirm → HandleConfirm ─────────────────────────────────────────────────┐
│  Overall progress bar: "Importing recipe N of M…"                         │
│  Per recipe:                                                               │
│    Phase 1 — Upload images (shown live in row):                           │
│      for each PendingImageData:                                           │
│        ProgressDetail = "Uploading image 2 / 4…"  → StateHasChanged()   │
│        RecipeImageService.UploadAsync(MemoryStream(bytes), name, recipe.Id)│
│        → collect Cloudinary URLs                                          │
│      recipe.Images = [url1, url2, ...]                                   │
│      recipe.HeroImageIndex = 0                                            │
│    Phase 2 — Save to CosmosDB (single write):                            │
│      ProgressDetail = "Saving…"  → StateHasChanged()                    │
│      RecipeService.AddAsync(recipe)  ← preserves pre-generated Id        │
│      row shows ✅ Saved  or  ❌ Failed: …                                │
└───────────────────────────────────────────────────────────────────────────┘
```

---

## Step-by-Step File Changes

### 1 — `Broccoli.App.Shared/Models/Recipe.cs`

Add `HeroImageIndex` and a non-serialised helper:

```csharp
/// <summary>Index into Images[] that designates the hero/primary photo. Defaults to 0.</summary>
[JsonPropertyName("heroImageIndex")]
public int HeroImageIndex { get; set; } = 0;

/// <summary>Convenience accessor: the hero image URL, falling back to the first image.</summary>
[JsonIgnore]
public string? HeroImageUrl =>
    Images.ElementAtOrDefault(HeroImageIndex) ?? Images.FirstOrDefault();
```

No schema migration needed — old documents without `heroImageIndex` deserialise to `0`, which
points to `Images[0]` as before.

---

### 2 — `Broccoli.App.Shared/Slices/Recipes/CosmosRecipeService.cs`

In `AddAsync`, replace the unconditional ID assignment:

```csharp
// Before
recipe.Id = Guid.NewGuid().ToString();

// After — preserve a caller-supplied ID (e.g. from the import flow)
if (string.IsNullOrEmpty(recipe.Id))
    recipe.Id = Guid.NewGuid().ToString();
```

`Recipe.Id` already defaults to `Guid.NewGuid().ToString()` in the model constructor, so the
import flow's pre-generated ID is automatically preserved without any extra wiring.

---

### 3 — `Broccoli.App.Shared/wwwroot/js/folderImport.js` *(new file)*

Two exported functions under `window.folderImport`:

#### `getRelativePaths(inputId) → string[]`

```js
window.folderImport = {
    getRelativePaths: function (inputId) {
        const input = document.getElementById(inputId);
        if (!input || !input.files) return [];
        return Array.from(input.files).map(f => f.webkitRelativePath || f.name);
    },
    ...
};
```

Returns paths in the **same order** as the browser's `FileList`, which matches Blazor's
`IBrowserFile[]` order — enabling a direct index-to-index zip in C#.

#### `generateThumbnail(bytes, maxWidth, maxHeight) → Promise<string|null>`

```js
generateThumbnail: function (bytes, maxWidth, maxHeight) {
    return new Promise(function (resolve) {
        const blob = new Blob([new Uint8Array(bytes)], { type: 'image/jpeg' });
        const url  = URL.createObjectURL(blob);
        const img  = new Image();
        img.onload = function () {
            const scale = Math.min(maxWidth / img.width, maxHeight / img.height, 1);
            const w = Math.round(img.width  * scale);
            const h = Math.round(img.height * scale);
            const canvas = document.createElement('canvas');
            canvas.width = w; canvas.height = h;
            canvas.getContext('2d').drawImage(img, 0, 0, w, h);
            URL.revokeObjectURL(url);
            resolve(canvas.toDataURL('image/jpeg', 0.8));
        };
        img.onerror = function () { URL.revokeObjectURL(url); resolve(null); };
        img.src = url;
    });
}
```

Called once per recipe (hero image only) during `AdvanceToPreview` to produce a ~120×90 JPEG
thumbnail stored in `ImportRecipeResult.ImageThumbnailDataUri`.  
Full-resolution bytes are kept in `PendingImageData.Bytes` and only streamed to Cloudinary during
`HandleConfirm`.

The script must be referenced in both host projects:
- `Broccoli.App.Web/Components/App.razor` — add `<script src="_content/Broccoli.App.Shared/js/folderImport.js"></script>`
- Ignored silently in MAUI (same pattern as `imageDropZone.js`).

---

### 4 — `Broccoli.App.Shared/wwwroot/js/imageDropZone.js`

The drop handler currently replaces all files with a single-file `DataTransfer`.  
Update it to **append** the dropped file to the existing hidden `<InputFile>` so repeated drops
add images to the multi-image carousel rather than replacing the current one:

```js
// Replace the DataTransfer block in the drop handler:
const dt = new DataTransfer();
// Copy any files already in the input (existing carousel images initiated via drop)
if (input.files) {
    Array.from(input.files).forEach(f => dt.items.add(f));
}
dt.items.add(files[0]);   // add the newly dropped file
input.files = dt.files;
input.dispatchEvent(new Event('change', { bubbles: true }));
```

This keeps single-file-per-drop semantics (only `files[0]` from the drop event) while supporting
the "drop again to add another" pattern. The Blazor handler `HandleImageUpload` only ever reads
`e.File` (one file), so no other C# changes are needed here.

---

### 5 — `Broccoli.App.Shared/Models/ImportRecipeResult.cs`

Add the following to the existing file:

```csharp
/// <summary>Single image pending Cloudinary upload.</summary>
public record PendingImageData(string FileName, byte[] Bytes);

/// <summary>Tracks which save phase a result is currently in.</summary>
public enum ImportProgress { NotStarted, UploadingImages, SavingRecipe }
```

Add to `ImportRecipeResult` class:

```csharp
/// <summary>Images buffered during parse; uploaded to Cloudinary during HandleConfirm.</summary>
public List<PendingImageData> PendingImages { get; set; } = [];

/// <summary>
/// Tiny (~120×90) JPEG data URI generated client-side for the preview table.
/// Never uploaded to Cloudinary.
/// </summary>
public string? ImageThumbnailDataUri { get; set; }

/// <summary>Current save phase (drives the live-progress column).</summary>
public ImportProgress Progress { get; set; } = ImportProgress.NotStarted;

/// <summary>Human-readable detail for the current phase, e.g. "Uploading image 2 / 4…"</summary>
public string? ProgressDetail { get; set; }
```

---

### 6 — `Broccoli.App.Shared/Slices/Recipes/Import/IImportFormat.cs`

Add two members with **default interface implementations** so all existing formats compile with
zero changes:

```csharp
/// <summary>True when this format can import images alongside HTML files from a folder.</summary>
bool SupportsFolderImport => false;

/// <summary>
/// Folder-aware parse. Receives the list of all available image relative paths and
/// a resolver that returns raw bytes for a given path.
/// Default implementation: delegates to ParseAsync and returns an empty image list.
/// </summary>
Task<FolderParseResult> ParseFolderFileAsync(
    string html,
    IReadOnlyList<string> availableImagePaths,
    Func<string, Task<byte[]?>> resolveImageBytes)
    => ParseAsync(html)
       .ContinueWith(t => new FolderParseResult(t.Result, []));
```

Add the result record to the same file (or a companion `FolderParseResult.cs` in `Import/`):

```csharp
/// <summary>Result of a folder-aware parse: the recipe plus any buffered image data.</summary>
public record FolderParseResult(Recipe Recipe, List<PendingImageData> PendingImages);
```

---

### 7 — `Broccoli.App.Shared/Slices/Recipes/Import/PaprikaHtmlImportFormat.cs`

#### Override `SupportsFolderImport`

```csharp
public bool SupportsFolderImport => true;
```

#### Implement `ParseFolderFileAsync`

```csharp
public async Task<FolderParseResult> ParseFolderFileAsync(
    string html,
    IReadOnlyList<string> availableImagePaths,
    Func<string, Task<byte[]?>> resolveImageBytes)
{
    // 1. Parse recipe (reuse all existing AngleSharp logic via ParseAsync)
    var recipe = await ParseAsync(html);

    // 2. Re-parse just the hero image src to get the UUID folder
    var context  = BrowsingContext.New(AngleSharp.Configuration.Default);
    var document = await context.OpenAsync(req => req.Content(html));
    var heroSrc  = document.QuerySelector("[itemprop=\"image\"]")?.GetAttribute("src");

    if (string.IsNullOrEmpty(heroSrc))
        return new FolderParseResult(recipe, []);

    // heroSrc = "Images/A22628EB-.../91115C0C-....jpg"
    var parts = heroSrc.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length < 2) return new FolderParseResult(recipe, []);

    var uuid   = parts[1];                                    // the recipe-scoped UUID folder
    var prefix = $"Images/{uuid}/";

    // 3. Collect ALL image paths for this recipe: hero first, rest in order
    var allPaths = availableImagePaths
        .Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .OrderBy(p => p == heroSrc ? 0 : 1)     // hero first
        .ThenBy(p => p)
        .ToList();

    // 4. Resolve bytes for each image
    var pendingImages = new List<PendingImageData>();
    foreach (var path in allPaths)
    {
        var bytes = await resolveImageBytes(path);
        if (bytes is { Length: > 0 })
            pendingImages.Add(new PendingImageData(Path.GetFileName(path), bytes));
    }

    return new FolderParseResult(recipe, pendingImages);
}
```

#### Update `ExportInstructions`

Add a folder-export path to the existing instructions:

```csharp
public IReadOnlyList<string> ExportInstructions =>
[
    "Open Paprika on your device.",
    "Select the recipes you want to export (or use Edit → Select All).",
    "Tap the Share / Export button.",
    "Choose \"HTML\" as the export format.",
    "Paprika will create a folder containing all recipe .html files and an Images/ subfolder.",
    "Use \"Folder\" mode below and select that export folder — all images are imported automatically.",
    "Or use \"Individual Files\" mode to drag individual .html files (images will not be imported)."
];
```

---

### 8 — `Broccoli.App.Shared/Slices/Recipes/Import/RecipeImportService.cs`

Add `ParseFilesWithImagesAsync` alongside the existing `ParseFilesAsync`:

```csharp
/// <summary>
/// Folder-mode parse: processes HTML files with access to an image file map.
/// Image bytes are buffered into ImportRecipeResult.PendingImages for upload during save.
/// </summary>
public async Task<List<ImportRecipeResult>> ParseFilesWithImagesAsync(
    IImportFormat format,
    IEnumerable<(string FileName, string Content)> htmlFiles,
    IReadOnlyList<string> availableImagePaths,
    Func<string, Task<byte[]?>> resolveImageBytes,
    IEnumerable<string> existingRecipeNames)
{
    var nameSet = existingRecipeNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var results = new List<ImportRecipeResult>();

    foreach (var (fileName, content) in htmlFiles)
    {
        if (content.StartsWith("__READ_ERROR__:"))
        {
            results.Add(new ImportRecipeResult
            {
                FileName     = fileName,
                Status       = ImportStatus.ParseError,
                ErrorMessage = content["__READ_ERROR__:".Length..],
                IsSelected   = false
            });
            continue;
        }

        try
        {
            var parsed   = await format.ParseFolderFileAsync(content, availableImagePaths, resolveImageBytes);
            var recipe   = parsed.Recipe;
            var isDup    = nameSet.Contains(recipe.Name);

            results.Add(new ImportRecipeResult
            {
                FileName      = fileName,
                Recipe        = recipe,
                Status        = isDup ? ImportStatus.Duplicate : ImportStatus.ReadyToImport,
                IsSelected    = !isDup,
                PendingImages = parsed.PendingImages
                // ImageThumbnailDataUri is set later by the dialog (requires JS interop)
            });
        }
        catch (Exception ex)
        {
            results.Add(new ImportRecipeResult
            {
                FileName     = fileName,
                Status       = ImportStatus.ParseError,
                ErrorMessage = ex.Message,
                IsSelected   = false
            });
        }
    }

    return results;
}
```

---

### 9 — `ImportRecipesDialog.razor` + `.razor.cs`

#### New injected services (`.razor.cs`)

```csharp
[Inject] private IRecipeImageService RecipeImageService { get; set; } = null!;
[Inject] private IJSRuntime JSRuntime { get; set; } = null!;
```

#### New state fields (`.razor.cs`)

```csharp
private bool _folderMode;
private Dictionary<string, IBrowserFile> _imageFileMap = [];
private IReadOnlyList<string> _availableImagePaths = [];
private int _importTotal;
private int _importCurrent;
```

#### `OnFormatChanged` — reset folder mode when format changes (`.razor.cs`)

```csharp
_folderMode = false;
```

#### New `HandleFolderChanged` (`.razor.cs`)

```csharp
private async Task HandleFolderChanged(InputFileChangeEventArgs e)
{
    var allFiles = e.GetMultipleFiles(10_000);
    var paths    = await JSRuntime.InvokeAsync<string[]>(
                       "folderImport.getRelativePaths", "folder-input");

    var htmlFiles = new List<IBrowserFile>();
    var imageMap  = new Dictionary<string, IBrowserFile>(StringComparer.OrdinalIgnoreCase);

    for (int i = 0; i < allFiles.Count; i++)
    {
        // Normalise the relative path; strip the top-level folder name
        // so "Recipes/Images/UUID/img.jpg" becomes "Images/UUID/img.jpg"
        var rel = (i < paths.Length ? paths[i] : allFiles[i].Name)
                  .Replace('\\', '/');
        var slashIdx = rel.IndexOf('/');
        if (slashIdx >= 0) rel = rel[(slashIdx + 1)..];

        if (rel.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            htmlFiles.Add(allFiles[i]);
        else if (rel.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
            imageMap[rel] = allFiles[i];
    }

    _allFolderHtmlFiles   = htmlFiles;          // List<IBrowserFile>
    _imageFileMap         = imageMap;
    _availableImagePaths  = imageMap.Keys.ToArray();
    _selectedFileCount    = htmlFiles.Count;
}
```

(Add `private List<IBrowserFile> _allFolderHtmlFiles = [];` to the state fields.)

#### `AdvanceToPreview` — branch on mode (`.razor.cs`)

```csharp
private async Task AdvanceToPreview()
{
    if (_activeFormat is null) return;
    if (!_folderMode && _selectedFiles.Count == 0) return;
    if (_folderMode  && _allFolderHtmlFiles.Count == 0) return;

    _isParsing = true;
    StateHasChanged();

    try
    {
        var existingNames = ExistingRecipes.Select(r => r.Name);

        if (_folderMode)
        {
            // ── Folder mode ──────────────────────────────────────────────
            var htmlData = new List<(string FileName, string Content)>();
            foreach (var file in _allFolderHtmlFiles)
            {
                try
                {
                    using var stream = file.OpenReadStream(10 * 1024 * 1024);
                    using var reader = new StreamReader(stream);
                    htmlData.Add((file.Name, await reader.ReadToEndAsync()));
                }
                catch (Exception ex)
                {
                    htmlData.Add((file.Name, $"__READ_ERROR__:{ex.Message}"));
                }
            }

            // Image resolver: opens IBrowserFile stream → byte[]
            async Task<byte[]?> Resolver(string relativePath)
            {
                if (!_imageFileMap.TryGetValue(relativePath, out var imgFile))
                    return null;
                using var s  = imgFile.OpenReadStream(5 * 1024 * 1024);
                using var ms = new MemoryStream();
                await s.CopyToAsync(ms);
                return ms.ToArray();
            }

            _importResults = await ImportService.ParseFilesWithImagesAsync(
                _activeFormat, htmlData, _availableImagePaths, Resolver, existingNames);

            // Generate thumbnails via JS Canvas for recipes that have images
            foreach (var result in _importResults.Where(r => r.PendingImages.Count > 0))
            {
                try
                {
                    result.ImageThumbnailDataUri = await JSRuntime.InvokeAsync<string?>(
                        "folderImport.generateThumbnail",
                        result.PendingImages[0].Bytes, 120, 90);
                }
                catch { /* non-fatal — preview just shows placeholder */ }
            }
        }
        else
        {
            // ── Individual files mode (unchanged) ────────────────────────
            var fileData = new List<(string FileName, string Content)>();
            foreach (var file in _selectedFiles)
            {
                try
                {
                    using var stream = file.OpenReadStream(10 * 1024 * 1024);
                    using var reader = new StreamReader(stream);
                    fileData.Add((file.Name, await reader.ReadToEndAsync()));
                }
                catch (Exception ex)
                {
                    fileData.Add((file.Name, $"__READ_ERROR__:{ex.Message}"));
                }
            }
            _importResults = await ImportService.ParseFilesAsync(
                _activeFormat, fileData, existingNames);
        }
    }
    catch (Exception ex)
    {
        _importResults = _selectedFiles
            .Select(f => new ImportRecipeResult
            {
                FileName     = f.Name,
                Status       = ImportStatus.ParseError,
                ErrorMessage = ex.Message,
                IsSelected   = false
            })
            .ToList();
    }
    finally
    {
        _isParsing = false;
        _currentStep = 2;
    }
}
```

#### `HandleConfirm` — upload then save (`.razor.cs`)

```csharp
private async Task HandleConfirm()
{
    _isSaving = true;
    StateHasChanged();

    var toSave = _importResults
        .Where(r => r.IsSelected && r.Recipe is not null)
        .ToList();

    _importTotal   = toSave.Count;
    _importCurrent = 0;
    StateHasChanged();

    foreach (var result in toSave)
    {
        _importCurrent++;

        // Phase 1: upload images to Cloudinary
        if (result.PendingImages.Count > 0)
        {
            result.Progress = ImportProgress.UploadingImages;
            var urls = new List<string>();

            for (int i = 0; i < result.PendingImages.Count; i++)
            {
                result.ProgressDetail = $"Uploading image {i + 1} / {result.PendingImages.Count}…";
                StateHasChanged();

                try
                {
                    var pending = result.PendingImages[i];
                    await using var ms  = new MemoryStream(pending.Bytes);
                    var url = await RecipeImageService.UploadAsync(ms, pending.FileName, result.Recipe!.Id);
                    urls.Add(url);
                }
                catch (Exception ex)
                {
                    // Non-fatal: log and continue; recipe saves without that image
                    Console.WriteLine($"Image upload failed: {ex.Message}");
                }
            }

            result.Recipe!.Images         = urls;
            result.Recipe.HeroImageIndex  = 0;
        }

        // Phase 2: save recipe (pre-generated Id is preserved by AddAsync)
        result.Progress      = ImportProgress.SavingRecipe;
        result.ProgressDetail = "Saving…";
        StateHasChanged();

        try
        {
            await RecipeService.AddAsync(result.Recipe!);
            result.SaveSuccess = true;
        }
        catch (Exception ex)
        {
            result.SaveSuccess = false;
            result.SaveError   = ex.Message;
        }

        result.ProgressDetail = null;
        StateHasChanged();
    }

    _isSaving     = false;
    _saveComplete = true;
    StateHasChanged();
}
```

#### `ResetState` — clear new fields (`.razor.cs`)

```csharp
_folderMode          = false;
_allFolderHtmlFiles  = [];
_imageFileMap        = [];
_availableImagePaths = [];
_importTotal         = 0;
_importCurrent       = 0;
```

#### Step 1 — Folder toggle + drop zone (`.razor`)

Below the format dropdown, when `_activeFormat.SupportsFolderImport`:

```razor
@if (_activeFormat?.SupportsFolderImport == true)
{
    <div class="import-mode-toggle">
        <button class="mode-btn @(!_folderMode ? "mode-btn-active" : "")"
                type="button" @onclick="() => _folderMode = false">
            📄 Individual Files
        </button>
        <button class="mode-btn @(_folderMode ? "mode-btn-active" : "")"
                type="button" @onclick="() => _folderMode = true">
            📁 Folder
        </button>
    </div>
}
```

When `_folderMode`, replace the existing `<InputFile>` drop zone with:

```razor
<div class="drop-zone @(_selectedFileCount > 0 ? "drop-zone-filled" : "")">
    <InputFile id="folder-input"
               webkitdirectory
               multiple
               class="drop-zone-input"
               OnChange="HandleFolderChanged" />
    <div class="drop-zone-content">
        @if (_selectedFileCount > 0)
        {
            <span class="drop-icon">✅</span>
            <p class="drop-label">
                <strong>@_selectedFileCount recipe@(_selectedFileCount == 1 ? "" : "s")</strong>
                + @_imageFileMap.Count image@(_imageFileMap.Count == 1 ? "" : "s") found
            </p>
            <p class="drop-sublabel">Click to change selection</p>
        }
        else
        {
            <span class="drop-icon">📁</span>
            <p class="drop-label">Drop your <strong>Paprika export folder</strong> here</p>
            <p class="drop-sublabel">or click to browse · selects the whole folder including images</p>
        }
    </div>
</div>
```

#### Step 2 — Preview table changes (`.razor`)

Add `col-thumb` as the **second** column (after the checkbox):

```razor
<th class="col-thumb">Image</th>
```

```razor
<td class="col-thumb">
    @if (result.ImageThumbnailDataUri is not null)
    {
        <div class="thumb-wrapper">
            <img src="@result.ImageThumbnailDataUri" class="import-thumb" alt="" />
            @if (result.PendingImages.Count > 1)
            {
                <span class="thumb-count">+@(result.PendingImages.Count - 1)</span>
            }
        </div>
    }
    else
    {
        <span class="text-muted">—</span>
    }
</td>
```

Add an **overall progress bar** above the table (shown while `_isSaving`):

```razor
@if (_isSaving && _importTotal > 0)
{
    <div class="import-overall-progress">
        <div class="import-progress-track">
            <div class="import-progress-fill"
                 style="width: @(_importCurrent * 100 / _importTotal)%"></div>
        </div>
        <span class="import-progress-label">
            Importing recipe @_importCurrent of @_importTotal…
        </span>
    </div>
}
```

Extend the **status column** to show live per-recipe progress:

```razor
<!-- At the top of the status <td>, before the existing switch/SaveSuccess logic -->
@if (result.Progress != ImportProgress.NotStarted && result.SaveSuccess == null)
{
    <span class="status-badge badge-saving">
        @(result.Progress == ImportProgress.UploadingImages ? "🖼️" : "💾")
        @result.ProgressDetail
    </span>
}
else
{
    <!-- existing SaveSuccess / Status switch unchanged -->
}
```

---

### 10 — `RecipeDetail.razor` + `.razor.cs` — Multi-image carousel

#### New state fields (`.razor.cs`)

```csharp
private int _carouselIndex;
```

#### Method changes (`.razor.cs`)

**`HandleImageUpload`** — append instead of replace:

```csharp
// Remove: recipe.Images.Clear();
recipe.Images.Add(url);
_carouselIndex = recipe.Images.Count - 1;   // navigate to newly added image
```

**`RemoveImage(int index)`** — replace parameterless overload:

```csharp
private async Task RemoveImage(int index)
{
    if (recipe == null || index < 0 || index >= recipe.Images.Count) return;

    var url = recipe.Images[index];
    recipe.Images.RemoveAt(index);

    // Keep HeroImageIndex valid
    if (recipe.HeroImageIndex >= recipe.Images.Count)
        recipe.HeroImageIndex = Math.Max(0, recipe.Images.Count - 1);
    else if (recipe.HeroImageIndex > index)
        recipe.HeroImageIndex--;

    // Keep carousel in bounds
    _carouselIndex = Math.Min(_carouselIndex, Math.Max(0, recipe.Images.Count - 1));

    try { await RecipeImageService.DeleteAsync(url); }
    catch (Exception ex) { Console.WriteLine($"Failed to delete image: {ex.Message}"); }
}
```

**New `SetHeroImage(int index)`:**

```csharp
private void SetHeroImage(int index)
{
    if (recipe == null) return;
    recipe.HeroImageIndex = index;
}
```

**New `NavigateCarousel(int delta)`:**

```csharp
private void NavigateCarousel(int delta)
{
    if (recipe == null || recipe.Images.Count == 0) return;
    _carouselIndex = (_carouselIndex + delta + recipe.Images.Count) % recipe.Images.Count;
}
```

#### Carousel HTML (`.razor`) — replace the existing image section

```razor
<!-- Image Section -->
<div class="form-section image-section">
    @if (recipe.Images.Count > 0)
    {
        <div class="image-carousel" id="recipe-drop-zone">
            <!-- Slide -->
            <div class="carousel-slide">
                <img src="@recipe.Images[_carouselIndex]" alt="Recipe" />
                @if (isUploadingImage)
                {
                    <div class="carousel-upload-overlay">
                        <div class="drop-zone-spinner"></div>
                        <span>Uploading…</span>
                    </div>
                }
            </div>

            <!-- Arrows (only when > 1 image) -->
            @if (recipe.Images.Count > 1)
            {
                <button type="button" class="carousel-arrow carousel-arrow-left"
                        @onclick="() => NavigateCarousel(-1)">‹</button>
                <button type="button" class="carousel-arrow carousel-arrow-right"
                        @onclick="() => NavigateCarousel(1)">›</button>
            }

            <!-- Counter + Hero badge -->
            <div class="carousel-counter">
                @(_carouselIndex + 1) / @recipe.Images.Count
                @if (_carouselIndex == recipe.HeroImageIndex)
                {
                    <span class="hero-badge">⭐ Hero</span>
                }
            </div>

            <!-- Dot indicators -->
            @if (recipe.Images.Count > 1)
            {
                <div class="carousel-dots">
                    @for (int i = 0; i < recipe.Images.Count; i++)
                    {
                        var idx = i;
                        <button type="button"
                                class="carousel-dot @(i == _carouselIndex ? "dot-active" : "") @(i == recipe.HeroImageIndex ? "dot-hero" : "")"
                                @onclick="() => _carouselIndex = idx"
                                title="@(i == recipe.HeroImageIndex ? "Hero image" : $"Image {i+1}")">
                        </button>
                    }
                </div>
            }
        </div>

        <!-- Per-image actions -->
        <div class="carousel-actions">
            @if (_carouselIndex != recipe.HeroImageIndex)
            {
                <button type="button" class="btn btn-secondary btn-sm"
                        @onclick="() => SetHeroImage(_carouselIndex)">
                    ⭐ Set as Hero
                </button>
            }
            <button type="button" class="btn btn-danger btn-sm"
                    @onclick="() => RemoveImage(_carouselIndex)"
                    disabled="@isUploadingImage">
                🗑 Remove
            </button>
            <label class="btn btn-secondary btn-sm" for="recipe-image-input">
                + Add Image
            </label>
        </div>
    }
    else
    {
        <!-- No image: full drop zone -->
        <div class="recipe-image-drop-zone" id="recipe-drop-zone">
            @if (isUploadingImage)
            {
                <div class="drop-zone-placeholder">
                    <div class="drop-zone-spinner"></div>
                    <span>Uploading…</span>
                </div>
            }
            else
            {
                <label class="drop-zone-placeholder" for="recipe-image-input">
                    <span class="drop-zone-icon">🖼️</span>
                    <strong>Drop image here</strong>
                    <span class="drop-zone-hint">or click to browse · JPG / PNG · max 5 MB</span>
                </label>
            }
        </div>
    }

    <!-- Always in DOM for drop zone and "Add Image" label target -->
    <InputFile id="recipe-image-input"
               OnChange="HandleImageUpload"
               accept=".jpg,.jpeg,.png"
               class="drop-zone-input-hidden"
               disabled="@isUploadingImage" />

    @if (!string.IsNullOrEmpty(imageUploadError))
    {
        <div class="alert alert-danger mt-2">@imageUploadError</div>
    }
</div>
```

---

### 11 — `RecipeReadOnly.razor` — Read-only carousel

Add `_readCarouselIndex` state to `RecipeReadOnly.razor.cs`:

```csharp
private int _readCarouselIndex;
```

Replace the hero image block in the `.razor`:

```razor
@if (recipe.Images.Count > 0)
{
    <!-- Reorder: hero first, then rest -->
    var orderedImages = recipe.Images
        .Select((url, i) => (url, i))
        .OrderBy(x => x.i == recipe.HeroImageIndex ? 0 : 1)
        .ThenBy(x => x.i)
        .Select(x => x.url)
        .ToList();

    <div class="read-carousel">
        <img src="@orderedImages[_readCarouselIndex]" alt="@recipe.Name" class="read-hero-img" />

        @if (orderedImages.Count > 1)
        {
            <button class="carousel-arrow carousel-arrow-left"
                    @onclick="() => _readCarouselIndex = (_readCarouselIndex - 1 + orderedImages.Count) % orderedImages.Count">
                ‹
            </button>
            <button class="carousel-arrow carousel-arrow-right"
                    @onclick="() => _readCarouselIndex = (_readCarouselIndex + 1) % orderedImages.Count">
                ›
            </button>
            <div class="carousel-counter">@(_readCarouselIndex + 1) / @orderedImages.Count</div>
        }
    </div>
}
```

---

### 12 — `Recipes.razor` — Hero image for cards

Change the card thumbnail from:

```razor
<img src="@recipe.Images.First()" alt="@recipe.Name" />
```

to:

```razor
<img src="@recipe.HeroImageUrl" alt="@recipe.Name" />
```

---

### 13 — CSS additions

#### `ImportRecipesDialog.razor.css`

```css
/* ── Import mode toggle ───────────────────────────────────────────────── */
.import-mode-toggle {
    display: flex;
    gap: 0;
    border: 1px solid #dee2e6;
    border-radius: 6px;
    overflow: hidden;
}
.mode-btn {
    flex: 1;
    padding: 0.4rem 0.75rem;
    font-size: 0.875rem;
    font-weight: 500;
    background: #f8f9fa;
    border: none;
    cursor: pointer;
    transition: background 0.15s, color 0.15s;
    color: #495057;
}
.mode-btn-active {
    background: #198754;
    color: #fff;
}

/* ── Thumbnail column ─────────────────────────────────────────────────── */
.col-thumb   { width: 64px; text-align: center; }
.thumb-wrapper { position: relative; display: inline-block; }
.import-thumb {
    width: 48px;
    height: 36px;
    object-fit: cover;
    border-radius: 4px;
    display: block;
}
.thumb-count {
    position: absolute;
    bottom: 2px;
    right: 2px;
    background: rgba(0,0,0,0.55);
    color: #fff;
    font-size: 0.62rem;
    padding: 0 3px;
    border-radius: 3px;
    line-height: 1.4;
}

/* ── Overall progress bar ─────────────────────────────────────────────── */
.import-overall-progress {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}
.import-progress-track {
    height: 6px;
    background: #e9ecef;
    border-radius: 3px;
    overflow: hidden;
}
.import-progress-fill {
    height: 100%;
    background: #198754;
    border-radius: 3px;
    transition: width 0.3s ease;
}
.import-progress-label {
    font-size: 0.82rem;
    color: #495057;
    margin: 0;
}
```

#### `RecipeDetail.razor.css`

```css
/* ── Multi-image carousel ─────────────────────────────────────────────── */
.image-carousel {
    position: relative;
    border-radius: 10px;
    overflow: hidden;
    background: #f8f9fa;
    margin-bottom: 0.75rem;
}
.carousel-slide img {
    width: 100%;
    height: 300px;
    object-fit: cover;
    display: block;
}
.carousel-upload-overlay {
    position: absolute;
    inset: 0;
    background: rgba(0,0,0,0.45);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: #fff;
    gap: 0.5rem;
}
.carousel-arrow {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    background: rgba(0,0,0,0.45);
    color: #fff;
    border: none;
    border-radius: 50%;
    width: 36px;
    height: 36px;
    font-size: 1.4rem;
    line-height: 1;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    transition: background 0.15s;
}
.carousel-arrow:hover { background: rgba(0,0,0,0.7); }
.carousel-arrow-left  { left: 8px; }
.carousel-arrow-right { right: 8px; }

.carousel-counter {
    position: absolute;
    top: 8px;
    right: 10px;
    background: rgba(0,0,0,0.5);
    color: #fff;
    font-size: 0.78rem;
    padding: 2px 8px;
    border-radius: 20px;
    display: flex;
    align-items: center;
    gap: 0.4rem;
}
.hero-badge {
    font-size: 0.72rem;
    color: #ffd700;
}

.carousel-dots {
    position: absolute;
    bottom: 8px;
    left: 50%;
    transform: translateX(-50%);
    display: flex;
    gap: 5px;
}
.carousel-dot {
    width: 8px;
    height: 8px;
    border-radius: 50%;
    border: none;
    background: rgba(255,255,255,0.5);
    cursor: pointer;
    padding: 0;
    transition: background 0.15s, transform 0.15s;
}
.carousel-dot.dot-active  { background: #fff; transform: scale(1.25); }
.carousel-dot.dot-hero    { background: #ffd700; }
.carousel-dot.dot-active.dot-hero { background: #ffd700; transform: scale(1.25); }

.carousel-actions {
    display: flex;
    gap: 0.5rem;
    flex-wrap: wrap;
    margin-bottom: 0.75rem;
}

/* Hidden file input used by "Add Image" label */
.drop-zone-input-hidden {
    position: absolute;
    width: 1px;
    height: 1px;
    opacity: 0;
    pointer-events: none;
}
```

---

## Testing Checklist

### Multi-image detail page
- [ ] Upload first image: shown in carousel, `HeroImageIndex = 0`
- [ ] Upload second image: carousel navigates to it, dots show 2 images
- [ ] Set hero on image 2: gold dot appears, counter shows ⭐ Hero
- [ ] Remove hero image: `HeroImageIndex` resets to 0, image deleted from Cloudinary
- [ ] Remove last image: drop zone reappears
- [ ] Drag-drop onto carousel: file appended, not replaced
- [ ] Save recipe: `HeroImageIndex` persisted to CosmosDB
- [ ] Reload page: correct image shown at `_carouselIndex = 0`

### Folder import
- [ ] Select export folder: shows "N recipes + M images found"
- [ ] AdvanceToPreview: thumbnails appear in `col-thumb` column, `+N` count badge visible
- [ ] Recipes without images: `—` in thumb column
- [ ] Duplicate detection still works
- [ ] `HandleConfirm`: overall progress bar advances; each row shows "🖼️ Uploading image X / Y…" then "💾 Saving…" then "✅ Saved"
- [ ] Failed image upload: recipe still saves, that image slot is skipped
- [ ] Resulting recipe in list: hero thumbnail visible on card
- [ ] Resulting recipe detail: all images in carousel; `HeroImageIndex = 0`
- [ ] Individual files mode still works unchanged

### RecipeReadOnly
- [ ] Single image: no arrows, image displayed
- [ ] Multiple images: arrows appear, counter shows N / M
- [ ] Hero image shown first

### Recipes list
- [ ] Card uses `HeroImageUrl`, not `Images.First()`
- [ ] Old recipes (no `heroImageIndex` field): fallback to `Images[0]`

