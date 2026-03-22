# Feature: Markdown Rendering in Recipe Read-Only View

**Date:** 2026-03-22

## Overview

Add simple markdown rendering support to recipe text fields. Authors write raw markdown in the **Recipe Detail** edit form; the **Read-Only view** renders it as correctly formatted HTML. Uses the existing **Markdig** .NET library — no custom parser required.

Markdown will be applied to the `Directions` and `Notes` fields. The `Ingredients` field remains a plain parsed list (unaffected).

---

## Steps

### 1. Add Markdig NuGet package

**File:** `Broccoli.App.Shared/Broccoli.App.Shared.csproj`

Add a `<PackageReference>` for Markdig:

```xml
<PackageReference Include="Markdig" Version="0.38.*" />
```

Markdig is a pure .NET library — no platform-specific dependencies — so it works identically in both the Web and MAUI hosts with no extra wiring.

---

### 2. Create `MarkdownRenderer` component

**File:** `Broccoli.App.Shared/Components/MarkdownRenderer.razor` *(new file)*

A single-file component that:
- Accepts a `string? Content` parameter
- Builds a `MarkdownPipeline` using `MarkdownPipelineBuilder` with `.UseAdvancedExtensions()` and `.DisableHtml()` (prevents raw HTML injection from user input)
- Calls `Markdig.Markdown.ToHtml(Content, pipeline)` and renders the result as a `MarkupString`

Supported markdown elements (via Markdig's built-in parsers):
- `#`, `##`, `###` — headings
- `**bold**` / `__bold__` — bold text
- `*italic*` / `_italic_` — italic text
- `1.` numbered lists
- `-` / `*` bullet points

---

### 3. Update `RecipeReadOnly.razor`

**File:** `Broccoli.App.Shared/Pages/RecipeReadOnly.razor`

- Replace the plain `@directions` text node in the Instructions section with `<MarkdownRenderer Content="@directions" />`
- Add a **Notes** section below Instructions that renders `recipe.Notes` using `<MarkdownRenderer Content="@recipe.Notes" />` (only shown when `Notes` is non-empty)

---

### 4. Update `RecipeReadOnly.razor.cs`

**File:** `Broccoli.App.Shared/Pages/RecipeReadOnly.razor.cs`

- Remove the `ParseLines()` call for `directions` — Markdig handles block-level rendering, so `directions` should be assigned directly as the raw string from `recipe.Directions` (no line-splitting needed)

---

### 5. Update `RecipeDetail.razor` (edit form hints)

**File:** `Broccoli.App.Shared/Pages/RecipeDetail.razor`

Add a `<small class="form-text">` hint beneath the **Directions** and **Notes** `<InputTextArea>` controls to inform authors that markdown is supported, e.g.:

> *Markdown supported: `**bold**`, `*italic*`, `# Header`, `- bullet`, `1. numbered list`*

No changes to the textarea bindings or `Recipe` model are needed.

---

## Further Considerations

| # | Topic | Detail |
|---|-------|--------|
| 1 | **Ingredients field** | Remains a plain parsed list — no markdown applied. Confirm if this is correct. |
| 2 | **No model migration needed** | `Recipe.Directions` and `Recipe.Notes` already store plain text strings; existing CosmosDB data is forward-compatible. |
| 3 | **MAUI compatibility** | Markdig has no platform dependencies — works in both Web and MAUI hosts without additional DI registration. |
| 4 | **XSS safety** | `.DisableHtml()` on the pipeline prevents authors from embedding raw `<script>` or other HTML tags. |

