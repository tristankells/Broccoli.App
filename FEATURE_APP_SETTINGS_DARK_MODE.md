# Feature Plan: App Settings Dialog & Dark Mode

## Overview

Add a ⚙️ **Settings** button to the top header bar (visible on all authenticated pages) that opens a modal settings dialog. The dialog contains:
1. **Dark Mode** — a toggle switch that applies a dark theme across the entire application
2. **Logout** — same flow as the existing sidebar logout button (which is kept)

Dark mode is implemented using the **industry-standard CSS custom-properties + `data-theme` attribute** approach. User preference is persisted via the existing `ISecureStorageService`, with an OS-preference fallback via a JS `prefers-color-scheme` query when no saved preference exists.

---

## Decision Log

| # | Decision |
|---|---|
| 1 | **Keep** the existing Logout button in `NavMenu.razor` — no removal |
| 2 | Fall back to `prefers-color-scheme: dark` (via JS) when no saved theme preference exists |
| 3 | Migrate **all** hardcoded hex colours in every component/page CSS file now, not progressively |
| 4 | `IJSRuntime` is injected into `MainLayout`, not into `ThemeService` — keeps the singleton service testable |

---

## Architecture

```
IThemeService (Shared)
    └── ThemeService (Shared) — holds IsDarkMode, persists via ISecureStorageService

themeInterop.js (Shared/wwwroot/js/)
    ├── window.setAppTheme(theme)     — sets data-theme attribute on <html>
    └── window.getOsPrefersDark()     — returns window.matchMedia query result

MainLayout.razor (.cs)
    ├── Injects IThemeService + IJSRuntime
    ├── OnAfterRenderAsync (first render):
    │     1. bool osDark = await JS.InvokeAsync<bool>("window.getOsPrefersDark")
    │     2. await ThemeService.InitializeAsync(osDark)
    │     3. await JS.InvokeVoidAsync("window.setAppTheme", isDark ? "dark" : "light")
    │     4. Subscribe ThemeService.OnThemeChanged → HandleThemeChanged
    └── HandleThemeChanged → apply via JS + StateHasChanged

AppSettingsDialog (Shared/Components/)
    ├── IsOpen / OnClose parameters
    ├── Dark Mode toggle → ThemeService.SetThemeAsync(bool)
    └── Logout → AuthStateService.LogoutAsync() + NavigateTo("/login")
```

---

## CSS Token System

All tokens are declared on `:root` (light mode defaults). The `[data-theme="dark"]` selector overrides them. Every hardcoded hex colour in every `.razor.css` and `app.css` is replaced with a `var(--token)` call.

### Complete Token Reference

```css
/* ════════════════════════════════════════════════════════════
   :root  —  Light mode defaults
   ════════════════════════════════════════════════════════════ */
:root {
  /* ── Background layers ───────────────────────────────── */
  --color-bg-page:            #ffffff;   /* html/body base */
  --color-bg-surface:         #f8f9fa;   /* section/panel/empty-state bg */
  --color-bg-raised:          #ffffff;   /* modals, cards, recipe cards */
  --color-bg-subtle:          #f3f3f3;   /* spinner tracks, faintest tints */
  --color-bg-hover:           #e9ecef;   /* row/button hover */
  --color-bg-selected:        #f0f7ff;   /* selected rows */

  /* ── Borders ─────────────────────────────────────────── */
  --color-border:             #dee2e6;   /* standard card / input border */
  --color-border-input:       #ced4da;   /* native input / select border */
  --color-border-subtle:      #f0f0f0;   /* intra-list row dividers */
  --color-border-emphasis:    #adb5bd;   /* hover / active border */

  /* ── Text ────────────────────────────────────────────── */
  --color-text-heading:       #1a1a1a;   /* page headings, card titles */
  --color-text-body:          #212529;   /* regular content */
  --color-text-secondary:     #495057;   /* labels, subtitles */
  --color-text-muted:         #6c757d;   /* hints, meta, placeholders */
  --color-text-placeholder:   #adb5bd;   /* very-muted / disabled */

  /* ── Brand / Interactive ─────────────────────────────── */
  --color-primary:            #0066cc;   /* links, focus rings, primary btn */
  --color-primary-hover:      #0052a3;   /* primary hover */
  --color-primary-dark:       #004080;   /* info callout text */
  --color-primary-subtle:     #e8f0fb;   /* selected toggles, radio bg */
  --color-primary-wash:       #f0f7ff;   /* drag-over bg, tab-add hover */
  --color-primary-info-bg:    #e7f3ff;   /* total-time info box */

  /* ── Semantic: Success ───────────────────────────────── */
  --color-success:            #28a745;
  --color-success-emphasis:   #2e7d32;
  --color-success-bg:         #d4edda;
  --color-success-bg-subtle:  #e8f5e9;
  --color-success-text:       #155724;

  /* ── Semantic: Warning / Amber ───────────────────────── */
  --color-warning:            #f0ad4e;
  --color-warning-text:       #856404;
  --color-warning-bg:         #fff3cd;
  --color-warning-bg-subtle:  #fff8e1;
  --color-amber:              #f0ad00;   /* star / favourite */
  --color-amber-text:         #856404;
  --color-amber-bg:           #fff9e6;

  /* ── Semantic: Danger ────────────────────────────────── */
  --color-danger:             #dc3545;
  --color-danger-hover:       #c82333;
  --color-danger-dark:        #721c24;
  --color-danger-bg:          #f8d7da;
  --color-danger-border:      #f5c6cb;

  /* ── Semantic: Indigo (merged badge) ─────────────────── */
  --color-indigo:             #3730a3;
  --color-indigo-bg:          #e0e7ff;

  /* ── Overlay ─────────────────────────────────────────── */
  --color-overlay:            rgba(0, 0, 0, 0.45);
}

/* ════════════════════════════════════════════════════════════
   [data-theme="dark"]  —  Dark mode overrides
   ════════════════════════════════════════════════════════════ */
[data-theme="dark"] {
  /* ── Background layers ───────────────────────────────── */
  --color-bg-page:            #0d1117;
  --color-bg-surface:         #161b22;
  --color-bg-raised:          #1e2530;
  --color-bg-subtle:          #21262d;
  --color-bg-hover:           #21262d;
  --color-bg-selected:        #1f3a5f;

  /* ── Borders ─────────────────────────────────────────── */
  --color-border:             #30363d;
  --color-border-input:       #444c56;
  --color-border-subtle:      #21262d;
  --color-border-emphasis:    #586069;

  /* ── Text ────────────────────────────────────────────── */
  --color-text-heading:       #e6edf3;
  --color-text-body:          #c9d1d9;
  --color-text-secondary:     #8b949e;
  --color-text-muted:         #6e7681;
  --color-text-placeholder:   #484f58;

  /* ── Brand / Interactive ─────────────────────────────── */
  --color-primary:            #58a6ff;
  --color-primary-hover:      #79b8ff;
  --color-primary-dark:       #1f6feb;
  --color-primary-subtle:     #1f3a5f;
  --color-primary-wash:       #0d2440;
  --color-primary-info-bg:    #0d2440;

  /* ── Semantic: Success ───────────────────────────────── */
  --color-success:            #3fb950;
  --color-success-emphasis:   #56d364;
  --color-success-bg:         #12261e;
  --color-success-bg-subtle:  #0f2a1a;
  --color-success-text:       #3fb950;

  /* ── Semantic: Warning / Amber ───────────────────────── */
  --color-warning:            #e3b341;
  --color-warning-text:       #e3b341;
  --color-warning-bg:         #2d2010;
  --color-warning-bg-subtle:  #261c0a;
  --color-amber:              #e3b341;
  --color-amber-text:         #e3b341;
  --color-amber-bg:           #2a1f0a;

  /* ── Semantic: Danger ────────────────────────────────── */
  --color-danger:             #f85149;
  --color-danger-hover:       #ff6e63;
  --color-danger-dark:        #f85149;
  --color-danger-bg:          #2d1216;
  --color-danger-border:      #6e1e24;

  /* ── Semantic: Indigo ────────────────────────────────── */
  --color-indigo:             #8b97ff;
  --color-indigo-bg:          #1a1f5c;

  /* ── Overlay ─────────────────────────────────────────── */
  --color-overlay:            rgba(0, 0, 0, 0.65);
}
```

### Hardcoded-Hex → Token Mapping Reference

| Raw value | Semantic role | Token |
|---|---|---|
| `#ffffff` / `white` (modal/card) | raised surface | `var(--color-bg-raised)` |
| `#ffffff` / `white` (input bg) | input background | `var(--color-bg-raised)` |
| `#f8f9fa` | section / panel bg | `var(--color-bg-surface)` |
| `#f3f3f3` / `#fafafa` | spinner track / faint hover | `var(--color-bg-subtle)` |
| `#f0f0f0` | close-btn hover / divider | `var(--color-border-subtle)` |
| `#e9ecef` / `#e0e0e0` | hover bg | `var(--color-bg-hover)` |
| `#dee2e6` | standard border | `var(--color-border)` |
| `#ced4da` | input/select border | `var(--color-border-input)` |
| `#adb5bd` | emphasis border / placeholder text | `var(--color-text-placeholder)` |
| `#6c757d` | muted text | `var(--color-text-muted)` |
| `#495057` | secondary text | `var(--color-text-secondary)` |
| `#333` | body text (Login) | `var(--color-text-body)` |
| `#212529` | body text | `var(--color-text-body)` |
| `#1a1a1a` | heading text | `var(--color-text-heading)` |
| `#0066cc` | primary / links / focus | `var(--color-primary)` |
| `#0052a3` | primary hover | `var(--color-primary-hover)` |
| `#004080` | primary dark (info text) | `var(--color-primary-dark)` |
| `#e8f0fb` | primary subtle (selected) | `var(--color-primary-subtle)` |
| `#f0f7ff` | primary wash (drag bg) | `var(--color-primary-wash)` |
| `#e7f3ff` | total-time info box bg | `var(--color-primary-info-bg)` |
| `#28a745` | success green | `var(--color-success)` |
| `#2e7d32` | success dark | `var(--color-success-emphasis)` |
| `#d4edda` / `#d1e7dd` | success bg | `var(--color-success-bg)` |
| `#155724` | success text | `var(--color-success-text)` |
| `#e8f5e9` / `#f0f8f0` | success subtle bg | `var(--color-success-bg-subtle)` |
| `#ffc107` | pantry category accent | `var(--color-warning)` |
| `#f0ad4e` | warning | `var(--color-warning)` |
| `#856404` | warning text | `var(--color-warning-text)` |
| `#fff3cd` | warning bg | `var(--color-warning-bg)` |
| `#fff8e1` / `#fff9e6` / `#ffe082` | warning subtle | `var(--color-warning-bg-subtle)` |
| `#f0ad00` / `#f0ad4e` (stars) | amber / favourite | `var(--color-amber)` |
| `#fff9e6` (star chip bg) | amber bg | `var(--color-amber-bg)` |
| `#dc3545` | danger | `var(--color-danger)` |
| `#c82333` / `#c0392b` / `#d32f2f` | danger hover | `var(--color-danger-hover)` |
| `#721c24` | danger dark text | `var(--color-danger-dark)` |
| `#f8d7da` | danger bg | `var(--color-danger-bg)` |
| `#f5c6cb` | danger border | `var(--color-danger-border)` |
| `#3730a3` | indigo | `var(--color-indigo)` |
| `#e0e7ff` | indigo bg | `var(--color-indigo-bg)` |
| `rgba(0,0,0,0.45)` | modal overlay | `var(--color-overlay)` |

### Colours **not** tokenized (fixed-brand, always kept as-is)

| Value | Location | Reason |
|---|---|---|
| `linear-gradient(180deg, rgb(5,39,103) 0%, #3a0647 70%)` | `NavMenu.razor.css` `.sidebar` | Branding sidebar gradient |
| `linear-gradient(135deg, #667eea 0%, #764ba2 100%)` | `LoginLayout.razor.css`, `Routes.razor.css` | Branding splash gradient |
| `linear-gradient(135deg, #667eea 0%, #764ba2 100%)` | `Recipes.razor.css` `.recipe-placeholder` | Branding card placeholder |
| `rgba(0,102,204,0.xx)` | various drop-zone / focus shadow `rgba` | Semi-transparent primary, stays inline |
| Data-URI SVG fills | `NavMenu.razor.css` `.bi-*` icons | SVG markup, not worth tokenizing |

---

## Implementation Steps

### Step 1 — `IThemeService` + `ThemeService`

**File:** `Broccoli.App.Shared/Services/IThemeService.cs` *(new)*

```csharp
namespace Broccoli.App.Shared.Services;

public interface IThemeService
{
    event Action? OnThemeChanged;
    bool IsDarkMode { get; }
    Task InitializeAsync(bool osPrefersDark = false);
    Task SetThemeAsync(bool isDark);
}
```

**File:** `Broccoli.App.Shared/Services/ThemeService.cs` *(new)*

- Constructor: `ISecureStorageService`, `ILogger<ThemeService>`
- `const string ThemeKey = "AppTheme"`
- `InitializeAsync(bool osPrefersDark)`:
  1. Try `_secureStorage.GetAsync(ThemeKey)`
  2. If null/empty → use `osPrefersDark` as default (no write yet; only persisted on first explicit toggle)
  3. Otherwise deserialize saved bool
  4. Set `IsDarkMode` and fire `OnThemeChanged`
- `SetThemeAsync(bool isDark)`: set `IsDarkMode`, persist via `_secureStorage.SetAsync(ThemeKey, ...)`, fire `OnThemeChanged`

---

### Step 2 — `themeInterop.js`

**File:** `Broccoli.App.Shared/wwwroot/js/themeInterop.js` *(new)*

```javascript
window.setAppTheme = function (theme) {
    document.documentElement.setAttribute('data-theme', theme);
};

window.getOsPrefersDark = function () {
    return window.matchMedia &&
           window.matchMedia('(prefers-color-scheme: dark)').matches;
};
```

**Add `<script>` reference** (pattern matches existing `imageDropZone.js` reference):

- `Broccoli.App.Web/Components/App.razor` — add after the existing imageDropZone script tag:
  ```html
  <script src="_content/Broccoli.App.Shared/js/themeInterop.js"></script>
  ```

- `Broccoli.App/wwwroot/index.html` — add before `</body>`:
  ```html
  <script src="_content/Broccoli.App.Shared/js/themeInterop.js"></script>
  ```

---

### Step 3 — `AppSettingsDialog` Component

**Files (three-file pattern):**
- `Broccoli.App.Shared/Components/AppSettingsDialog.razor` *(new)*
- `Broccoli.App.Shared/Components/AppSettingsDialog.razor.cs` *(new)*
- `Broccoli.App.Shared/Components/AppSettingsDialog.razor.css` *(new)*

**`.razor` structure** — follows `MacroTargetSettingsDialog` modal pattern:
- Backdrop div + centred box div
- Header bar: "⚙️ App Settings" title + close button
- Body:
  - **Appearance section**: labelled toggle switch for Dark Mode, bound to `ThemeService.IsDarkMode`
  - **Account section**: "Logout" button (red/outlined, danger style)
- No footer (actions are inline per-section, no Save needed)

**`.razor.cs`:**
```csharp
[Parameter] public bool IsOpen { get; set; }
[Parameter] public EventCallback OnClose { get; set; }
[Inject] IThemeService ThemeService { get; set; } = null!;
[Inject] IAuthenticationStateService AuthStateService { get; set; } = null!;
[Inject] NavigationManager Navigation { get; set; } = null!;

private async Task ToggleDarkMode(bool isDark) =>
    await ThemeService.SetThemeAsync(isDark);

private async Task Logout()
{
    await OnClose.InvokeAsync();
    await AuthStateService.LogoutAsync();
    Navigation.NavigateTo("/login");
}
```

**`.razor.css`** — uses only `var(--token)` values; reuses `.modal-backdrop`, `.modal-dialog-container`, `.settings-modal-box`, `.modal-header-bar` patterns; adds a CSS toggle-switch for the dark mode row.

---

### Step 4 — `MainLayout.razor` + `MainLayout.razor.cs`

**`MainLayout.razor`** changes:
- Replace the `<a href="https://learn.microsoft.com/...">About</a>` link in `.top-row` with:
  ```razor
  <button class="settings-btn" @onclick="OpenSettings" title="App Settings">⚙️</button>
  <AppSettingsDialog IsOpen="_settingsOpen" OnClose="CloseSettings" />
  ```
- Add `@using Broccoli.App.Shared.Components` if not already in `_Imports.razor`

**`MainLayout.razor.cs`** *(new file — split from `.razor`)* or added as `@code {}`:

```csharp
[Inject] IThemeService ThemeService { get; set; } = null!;
[Inject] IJSRuntime JS { get; set; } = null!;

private bool _settingsOpen;
private bool _initialized;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender || _initialized) return;
    _initialized = true;

    bool osDark = await JS.InvokeAsync<bool>("window.getOsPrefersDark");
    await ThemeService.InitializeAsync(osDark);
    await JS.InvokeVoidAsync("window.setAppTheme",
        ThemeService.IsDarkMode ? "dark" : "light");

    ThemeService.OnThemeChanged += HandleThemeChanged;
    StateHasChanged();
}

private async void HandleThemeChanged()
{
    await JS.InvokeVoidAsync("window.setAppTheme",
        ThemeService.IsDarkMode ? "dark" : "light");
    await InvokeAsync(StateHasChanged);
}

private void OpenSettings()  => _settingsOpen = true;
private void CloseSettings() => _settingsOpen = false;

public void Dispose()
{
    ThemeService.OnThemeChanged -= HandleThemeChanged;
}
```

`MainLayout` must `@implement IDisposable` to unsubscribe.

**`MainLayout.razor.css`** changes:
- Replace all hardcoded colours with tokens (see Phase 7 below)
- Add `.settings-btn` styles: icon-only button, fits the existing `.top-row` height, uses token colours

---

### Step 5 — DI Registration

**`Broccoli.App.Web/Program.cs`** — add after `AuthenticationStateService` registration:
```csharp
builder.Services.AddSingleton<IThemeService, ThemeService>();
```

**`Broccoli.App/MauiProgram.cs`** — same:
```csharp
builder.Services.AddSingleton<IThemeService, ThemeService>();
```

No startup `InitializeAsync()` call needed in either host — initialization is deferred to `MainLayout.OnAfterRenderAsync` so the JS runtime is available.

---

## CSS Migration

> **Rule:** After migration, no file may contain a raw hex colour for any of the values in the token table above. The only remaining hex values permitted are `rgba(...)` for semi-transparent shadows/overlays with primary-colour tints, brand gradients listed in the "not tokenized" table, and SVG data URIs.

---

### Phase 6 — `Broccoli.App.Shared/wwwroot/app.css`

Changes:
1. Insert full `:root { }` and `[data-theme="dark"] { }` blocks at the very top (before all other rules).
2. Add `html, body { background: var(--color-bg-page); color: var(--color-text-body); }` (replaces generic `font-family` rule or extends it).
3. Replace `.btn-primary` colours and `.btn-link` / `a` colour with tokens.
4. Replace `.valid.modified` `#26b050` with `var(--color-success)`.
5. Replace `.invalid`, `.validation-message` `#e50000` with `var(--color-danger)`.
6. The `#blazor-error-ui` `lightyellow` background and `#b32121` error boundary are platform/framework UI — leave as-is or override with tokens (low priority, acceptable to leave).

---

### Phase 7 — Layout CSS

#### `MainLayout.razor.css`
| Rule | Change |
|---|---|
| `.top-row` `background-color: #f7f7f7` | `var(--color-bg-surface)` |
| `.top-row` `border-bottom: 1px solid #d6d5d5` | `1px solid var(--color-border)` |

#### `NavMenu.razor.css`
| Rule | Change |
|---|---|
| `.top-row` `background-color: rgba(0,0,0,0.4)` | unchanged (dark overlay on gradient — OK) |
| `.username` `color: white` | unchanged (always white on dark gradient sidebar) |
| `.nav-item ::deep .nav-link` `color: #d7d7d7` | unchanged (always light on dark gradient) |
| `.nav-item ::deep a.active` background `rgba(255,255,255,0.37)` | unchanged |
| (No neutral-palette hex values present) | — |

> **Note:** `NavMenu` uses only white/rgba values against the fixed dark sidebar gradient — no token migration needed.

#### `LoginLayout.razor.css`
No token migration needed (all values are part of the brand gradient).

#### `Routes.razor.css`
No token migration needed (loading screen uses brand gradient + white text).

---

### Phase 8 — Pages CSS

#### `Login.razor.css`
| Hardcoded | Token |
|---|---|
| `.login-card` `background-color: white` | `var(--color-bg-raised)` |
| `.login-card h2` `color: #333` | `var(--color-text-body)` |
| All input `border: ... #dee2e6` | `var(--color-border)` |
| All input `:focus border-color: #0066cc` | `var(--color-primary)` |
| Error/alert colours | tokens per table above |

#### `Recipes.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | `var(--color-text-heading)` |
| `.filters-section` `background: #f8f9fa` | `var(--color-bg-surface)` |
| `.search-box input` `border: 2px solid #dee2e6` | `var(--color-border)` |
| `.search-box input:focus border-color: #0066cc` | `var(--color-primary)` |
| `.tag-filter-label` `color: #495057` | `var(--color-text-secondary)` |
| `.tag-chip` `background: white`, `border: 2px solid #dee2e6`, `color: #495057` | `var(--color-bg-raised)`, `var(--color-border)`, `var(--color-text-secondary)` |
| `.tag-chip:hover` `border-color: #0066cc`, `color: #0066cc` | `var(--color-primary)` |
| `.tag-chip.selected` `background: #0066cc; border-color: #0066cc; color: white` | `var(--color-primary)` + `white` (text on primary is always white) |
| `.clear-filters` `color: #0066cc` | `var(--color-primary)` |
| `.recipe-stats` `color: #6c757d` | `var(--color-text-muted)` |
| `.loading-container` `color: #6c757d` | `var(--color-text-muted)` |
| `.spinner` `border: 4px solid #f3f3f3`, `border-top: 4px solid #0066cc` | `var(--color-bg-subtle)`, `var(--color-primary)` |
| `.empty-state` `background: #f8f9fa` | `var(--color-bg-surface)` |
| `.empty-state h2` `color: #495057` | `var(--color-text-secondary)` |
| `.empty-state p` `color: #6c757d` | `var(--color-text-muted)` |
| `.recipe-card` `background: white`, `box-shadow: 0 2px 8px rgba(...)` | `var(--color-bg-raised)` |
| `.recipe-image` `background: #f8f9fa` | `var(--color-bg-surface)` |
| `.recipe-name` `color: #1a1a1a` | `var(--color-text-heading)` |
| `.recipe-meta` `color: #6c757d` | `var(--color-text-muted)` |
| `.recipe-tag` `background: #e9ecef`, `color: #495057` | `var(--color-bg-hover)`, `var(--color-text-secondary)` |
| `.recipe-tag-more` `background: #dee2e6`, `color: #6c757d` | `var(--color-border)`, `var(--color-text-muted)` |
| `.recipe-actions` `border-top: 1px solid #e9ecef`, `background: #f8f9fa` | `var(--color-border)`, `var(--color-bg-surface)` |
| `.btn-icon` `background: white`, `border: 1px solid #dee2e6`, `color: #495057` | `var(--color-bg-raised)`, `var(--color-border)`, `var(--color-text-secondary)` |
| `.btn-icon:hover` `background: #e9ecef`, `border-color: #adb5bd` | `var(--color-bg-hover)`, `var(--color-border-emphasis)` |
| `.btn-icon.btn-danger` `color: #dc3545` | `var(--color-danger)` |
| `.btn-icon.btn-danger:hover` `background: #dc3545`, `border-color: #dc3545` | `var(--color-danger)` |
| `.btn-favorite` `color: #aaa` | `var(--color-text-placeholder)` |
| `.btn-favorite:hover`, `.btn-favorite.is-favorite` `color: #f0ad00` | `var(--color-amber)` |
| `.favorites-chip` `border-color: #f0ad00`, `color: #856404`, `background-color: #fff9e6` | `var(--color-amber)`, `var(--color-amber-text)`, `var(--color-amber-bg)` |
| `.favorites-chip.selected` `background-color: #f0ad00` | `var(--color-amber)` |
| `.favorite-badge` `color: #f0ad00` | `var(--color-amber)` |

#### `RecipeDetail.razor.css`
| Hardcoded | Token |
|---|---|
| `.detail-header h1` `color: #1a1a1a` | `var(--color-text-heading)` |
| `.btn-back` `background: #f8f9fa`, `border: 1px solid #dee2e6`, `color: #495057` | surface/border/secondary |
| `.btn-back:hover` `background: #e9ecef`, `border-color: #adb5bd` | hover/emphasis |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` track/top | subtle/primary |
| `.error-message` `background: #f8d7da`, `border: ... #f5c6cb`, `color: #721c24` | danger tokens |
| `.form-section` `background: white`, `border: 1px solid #dee2e6` | raised/border |
| `.form-section h2` `color: #1a1a1a` | heading |
| `.section-subtitle` `color: #6c757d` | muted |
| `.recipe-image-drop-zone` `border: 2px dashed #dee2e6`, `background: #f8f9fa` | border/surface |
| `.recipe-image-drop-zone:not(.has-image):hover border-color: #0066cc`, `background: #f0f7ff` | primary/wash |
| `.recipe-image-drop-zone.drag-over` `border-color: #0066cc`, `background-color: #e6f0ff` | primary/subtle |
| `.recipe-image-drop-zone.has-image border-color: #dee2e6` | border |
| `.drop-zone-placeholder` `color: #6c757d` | muted |
| `.drop-zone-hint` `color: #adb5bd` | placeholder |
| `.drop-zone-spinner` `border: 3px solid #dee2e6`, `border-top-color: #0066cc` | border/primary |
| `.form-group label` `color: #495057` | secondary |
| `.form-control` `border: 2px solid #dee2e6` | border |
| `.form-control:focus border-color: #0066cc` | primary |
| `.form-text` `color: #6c757d` | muted |
| `.total-time` `background: #e7f3ff`, `border-left: 4px solid #0066cc`, `color: #004080` | info-bg/primary/primary-dark |
| `.tag-chip` `background: #0066cc`, `color: white` | primary (text stays white on primary) |
| `.ingredients-parsed` `background: #f8f9fa` | surface |
| `.ingredients-parsed h3` `color: #495057` | secondary |
| `.ingredients-parsed li` `color: #495057` | secondary |
| `.nutrition-section` `background: #f8f9fa` | surface |
| `.form-actions` `background: white`, `border: 1px solid #dee2e6` | raised/border |
| `.btn-primary` `background: #0066cc` | primary |
| `.btn-primary:hover` `background: #0052a3` | primary-hover |
| `.btn-secondary` `background: #6c757d` | muted (or secondary btn token — keep `#6c757d` as it's Bootstrap secondary) |
| `.btn-danger` `background: #dc3545` | danger |
| `.btn-danger:hover` `background: #c82333` | danger-hover |
| `.alert-success` `background: #d4edda`, `color: #155724`, `border: ... #c3e6cb` | success-bg/text/border |
| `.alert-danger` `background: #f8d7da`, `color: #721c24`, `border: ... #f5c6cb` | danger-bg/dark/border |

#### `RecipeReadOnly.razor.css`
| Hardcoded | Token |
|---|---|
| `.btn-back` colours | same as RecipeDetail above |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | subtle/primary |
| `.error-message` | danger tokens |
| `.recipe-title` `color: #1a1a1a` | heading |
| `.ingredients-section`, `.instructions-section` `background: #ffffff`, `border: 1px solid #dee2e6` | raised/border |
| `h2` `color: #1a1a1a` | heading |
| `.ingredients-list li` `color: #333` | body |
| `.ingredients-list li` border-bottom `#f1f3f5` | subtle |
| `.ingredients-list li::before` `color: #0066cc` | primary |
| `.instruction-step` `color: #333`, `background: #f8f9fa`, `border-left: 4px solid #0066cc` | body/surface/primary |
| `.empty-text` `color: #6c757d` | muted |

#### `Foods.razor.css`
| Hardcoded | Token |
|---|---|
| `.edit-input` `border: 1px solid #ced4da` | `var(--color-border-input)` |
| `.btn-icon:hover` `background: #e9ecef` | `var(--color-bg-hover)` |
| `.btn-danger-icon:hover` `background: #f8d7da` | `var(--color-danger-bg)` |
| `.btn-success-icon:hover` `background: #d1e7dd` | `var(--color-success-bg)` |
| `.editing-row` `background-color: #fff3cd` | `var(--color-warning-bg)` |
| `.add-row` `background-color: #d1e7dd` | `var(--color-success-bg)` |
| `.delete-confirm-row` `background-color: #f8d7da` | `var(--color-danger-bg)` |

#### `GroceryList.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | heading |
| `.add-item-form .form-control` `border: 2px solid #dee2e6` | border |
| `.add-item-form .form-control:focus` `border-color: #0066cc` | primary |
| `.grocery-stats` `color: #6c757d` | muted |
| `.grocery-item` `border-bottom: 1px solid #f0f0f0` | subtle |
| `.grocery-item:hover` `background-color: #f8f9fa` | surface |
| `.grocery-item.checked .grocery-name` `color: #adb5bd` | placeholder |
| `.grocery-name` `color: #212529` | body |
| `.delete-btn` `color: #dc3545` | danger |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | subtle/primary |
| `.empty-state` `color: #6c757d` | muted |
| `.empty-state h2` `color: #495057` | secondary |

#### `Pantry.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | heading |
| `.form-control` `border: 2px solid #dee2e6` | border |
| `.form-control:focus` `border-color: #0066cc` | primary |
| `.category-select` `border: 2px solid #dee2e6` | border |
| `.always-have-badge` `background-color: #d4edda`, `color: #155724` | success-bg/text |
| `.check-if-have-badge` `background-color: #fff3cd`, `color: #856404` | warning-bg/text |
| `.category-count` `color: #6c757d` | muted |
| `.pantry-list` `border: 1px solid #e9ecef` | border |
| `.pantry-item` `border-bottom: 1px solid #f0f0f0` | subtle |
| `.pantry-item:hover` `background-color: #f8f9fa` | surface |
| `.pantry-item.always-have` `border-left: 4px solid #28a745` | success |
| `.pantry-item.check-if-have` `border-left: 4px solid #ffc107` | warning |
| `.pantry-name` `color: #212529` | body |
| `.category-dropdown` `border-color: #dee2e6`, `color: #495057` | border/secondary |
| `.delete-btn` `color: #dc3545` | danger |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | border/primary |
| `.empty-state` `color: #6c757d` | muted |
| `.empty-state h2` `color: #495057` | secondary |

#### `MacroTargets.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | heading |
| `.btn-primary` colours | primary/primary-hover |
| `.btn-outline` `background: #fff`, `color: #0066cc`, `border: 2px solid #0066cc` | raised/primary |
| `.btn-outline:hover` `background: #e8f0fb` | primary-subtle |
| `.alert-danger` | danger tokens |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | border/primary |
| `.empty-state` `color: #6c757d` | muted |
| `.empty-state h2` `color: #495057` | secondary |
| `.table-scroll-wrapper` `border: 1px solid #dee2e6` | border |
| `.macro-table` `background: #fff` | raised |
| `.macro-table thead tr` `background: #f8f9fa`, `border-bottom: 2px solid #dee2e6` | surface/border |
| `.macro-table th` `color: #495057` | secondary |
| `.unit-label` `color: #6c757d` | muted |
| `.macro-table tbody tr` `border-bottom: 1px solid #f0f0f0` | subtle |
| `.macro-table tbody tr:hover` `background: #fafafa` | subtle |
| `.calc-cell` `color: #495057` | secondary |
| `.calories-cell` `color: #0066cc` | primary |
| `.protein-cell` `color: #d9534f` | danger (close enough) |
| `.carbs-cell` `color: #f0ad4e` | warning |
| `.fat-cell` `color: #5cb85c` | success (close) |
| `.cell-input:focus`, `.cell-select:focus` `border-color: #0066cc` | primary |
| `.cell-input:hover`, `.cell-select:hover` `border-color: #adb5bd`, `background: #fff` | emphasis/raised |
| `.btn-danger:hover` `background: #f8d7da` | danger-bg |
| `.legend-item` `color: #6c757d` | muted |
| `.calories-dot` `background: #0066cc` | primary |
| `.protein-dot` `background: #d9534f` | danger |
| `.carbs-dot` `background: #f0ad4e` | warning |
| `.fat-dot` `background: #5cb85c` | success |

> Note: Macro-colour dots and cells (protein/carbs/fat) are semantic data-visualisation colours; keep consistent with or without dark mode. The token values chosen for dark mode are slightly lighter/adjusted versions for readability.

#### `MealPrepPlans.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | heading |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | subtle/primary |
| `.empty-state` `background: #f8f9fa` | surface |
| `.empty-state h2` `color: #495057` | secondary |
| `.empty-state p` `color: #6c757d` | muted |
| `.plan-card` `background: white`, `border: 1px solid #dee2e6` | raised/border |
| `.plan-card.drag-over` `border: 2px dashed #0066cc`, `background: #f0f7ff` | primary/wash |
| `.plan-header` `background: #f8f9fa`, `border-bottom: 1px solid transparent` | surface |
| `.drag-handle` `color: #adb5bd` | placeholder |
| `.plan-header:hover .drag-handle` `color: #6c757d` | muted |
| `.plan-card.is-expanded .plan-header border-bottom-color: #dee2e6` | border |
| `.plan-header:hover` `background: #e9ecef` | hover |
| `.expand-toggle` `color: #6c757d` | muted |
| `.plan-name` `color: #1a1a1a` | heading |
| `.plan-name-input` `color: #1a1a1a`, `border: 2px solid #0066cc`, `background: white` | heading/primary/raised |
| `.btn-icon-sm` `color: #6c757d` | muted |
| `.btn-icon-sm:hover` `background: #dee2e6`, `color: #212529` | border/body |
| `.no-plan-recipes` `color: #6c757d` | muted |
| `.plan-recipe-item` `border-bottom: 1px solid #f0f0f0` | subtle |
| `.plan-recipe-item:hover` `background: #f8f9fa` | surface |
| `.plan-recipe-name` `color: #212529` | body |
| `.plan-recipe-tag` `background: #e9ecef`, `color: #495057` | hover/secondary |

#### `DailyFoodPlanning.razor.css`
| Hardcoded | Token |
|---|---|
| `.page-header h1` `color: #1a1a1a` | heading |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | subtle/primary |
| `.empty-state` `background: #f8f9fa` | surface |
| `.empty-state h2` `color: #495057` | secondary |
| `.empty-state p` `color: #6c757d` | muted |
| `.plan-card` `background: white`, `border: 1px solid #dee2e6` | raised/border |
| `.plan-card:hover border-color: #0066cc` | primary |
| `.plan-card-name` `color: #1a1a1a` | heading |
| `.plan-card-meta` `color: #6c757d` | muted |
| `.editor-header border-bottom: 1px solid #dee2e6` | border |
| `.plan-title` `color: #1a1a1a` | heading |
| `.plan-name-input border: 2px solid #0066cc` | primary |
| `.save-indicator.saving` `color: #856404`, `background: #fff3cd` | warning-text/bg |
| `.save-indicator.saved` `color: #155724`, `background: #d4edda` | success-text/bg |
| `.save-indicator.failed` `color: #721c24`, `background: #f8d7da` | danger-dark/bg |
| `.tab-bar border-bottom: 2px solid #dee2e6` | border |
| `.tab-item` `background: #f8f9fa`, `color: #495057` | surface/secondary |
| `.tab-item:hover:not(.active)` `background: #e9ecef` | hover |
| `.tab-item.active` `background: white`, `border-color: #dee2e6`, `color: #0066cc` | raised/border/primary |
| `.tab-name-input border: 1px solid #0066cc` | primary |
| `.tab-add-btn border: 1px dashed #adb5bd`, `color: #6c757d` | emphasis/muted |
| `.tab-add-btn:hover border-color: #0066cc`, `color: #0066cc`, `background: #f0f7ff` | primary/wash |
| `.tab-person-bar` `background: #f8f9fa`, `border: 1px solid #dee2e6` | surface/border |
| `.person-label` `color: #495057` | secondary |
| `.person-select border: 1px solid #ced4da`, `background: white`, `color: #212529` | input/raised/body |
| `.table-wrapper border: 1px solid #dee2e6` | border |
| `.dfp-table thead th` `background: #f1f3f5`, `color: #495057`, `border-bottom: 2px solid #dee2e6` | surface/secondary/border |
| `.dfp-table td border-bottom: 1px solid #f1f3f5` | subtle |
| `.header-row td` `background: #e9f0fa` | primary-subtle |
| `.header-name-input` `color: #1a3a6b` (dark blue heading) | primary-dark (close) |
| `.header-name-input:focus` `background: #dce8fb` | primary-subtle |
| `.food-row:hover` `background: #fafafa` | subtle |
| `.item-select`, `.serving-input`, `.qty-input` `border: 1px solid #ced4da`, `background: white`, `color: #212529` | input/raised/body |
| `:focus border-color: #0066cc` | primary |
| `.drag-handle` `color: #adb5bd` | placeholder |
| `.row-drag-over td border-top: 2px solid #0066cc`, `background-color: #f0f7ff` | primary/wash |
| `.btn-icon-sm:hover background: #f8d7da` | danger-bg |
| `tfoot td` `background: #f8f9fa`, `border-top: 2px solid #dee2e6` | surface/border |
| `.totals-row td background: #f1f3f5` | subtle |
| `.to-target-row td background: #f8f9fa` | surface |
| `.to-target-label` `color: #6c757d` | muted |
| `.delta-green` `color: #1a7a3f` | success-emphasis (close) |
| `.delta-orange` `color: #856404` | warning-text |
| `.delta-red` `color: #c0392b` | danger-hover |
| `.empty-tabs-hint` `color: #6c757d` | muted |

---

### Phase 9 — Components CSS

#### `AddIngredientsDialog.razor.css`
| Hardcoded | Token |
|---|---|
| `.modal-content-box` `background: #fff` | raised |
| `.modal-header-bar border-bottom: 1px solid #e9ecef` | border |
| `.modal-title-text` `color: #1a1a1a` | heading |
| `.modal-close-btn` `color: #6c757d` | muted |
| `.modal-close-btn:hover` `color: #212529`, `background: #f0f0f0` | body/subtle |
| `.recipe-name-subtitle` `color: #6c757d` | muted |
| `.dialog-hint` `color: #6c757d` | muted |
| `.ingredient-row border-bottom: 1px solid #f0f0f0` | subtle |
| `.ingredient-row.unchecked-row .ingredient-text` `color: #adb5bd` | placeholder |
| `.ingredient-text` `color: #212529` | body |
| `.badge-always-have` `background-color: #d4edda`, `color: #155724` | success-bg/text |
| `.badge-check-if-have` `background-color: #fff3cd`, `color: #856404` | warning-bg/text |
| `.merged-badge` `background-color: #e0e7ff`, `color: #3730a3` | indigo-bg/indigo |
| `.modal-footer-bar border-top: 1px solid #e9ecef`, `background: #f8f9fa` | border/surface |
| `.loading-container` `color: #6c757d` | muted |
| `.spinner` | border/primary |

#### `AddRecipesToPlanDialog.razor.css`
| Hardcoded | Token |
|---|---|
| `.modal-content-box` `background: #fff` | raised |
| `.modal-header-bar border-bottom: 1px solid #e9ecef` | border |
| `.modal-title-text` `color: #1a1a1a` | heading |
| `.modal-close-btn` `color: #6c757d` | muted |
| `.modal-close-btn:hover` `color: #212529`, `background: #f0f0f0` | body/subtle |
| `.plan-name-subtitle` `color: #6c757d` | muted |
| `.recipe-search-input border: 1.5px solid #dee2e6` | border |
| `.recipe-search-input:focus border-color: #0066cc` | primary |
| `.no-recipes` `color: #6c757d` | muted |
| `.recipe-check-row border-bottom: 1px solid #f0f0f0` | subtle |
| `.recipe-check-row:hover` `background: #f8f9fa` | surface |
| `.recipe-check-row.row-selected` `background: #f0f7ff` | wash |
| `.recipe-check-name` `color: #212529` | body |
| `.recipe-check-tag` `background: #e9ecef`, `color: #495057` | hover/secondary |
| `.modal-footer-bar border-top: 1px solid #e9ecef`, `background: #f8f9fa` | border/surface |

#### `MacroTargetSettingsDialog.razor.css`
| Hardcoded | Token |
|---|---|
| `.settings-modal-box` `background: #fff` | raised |
| `.modal-title-text` `color: #1a1a1a` | heading |
| `.modal-close-btn` `color: #6c757d` | muted |
| `.modal-close-btn:hover` `color: #212529`, `background: #f0f0f0` | body/subtle |
| `.settings-section border-bottom: 1px solid #f0f0f0` | subtle |
| `.settings-section-title` `color: #6c757d` | muted |
| `.toggle-btn` `border: 2px solid #dee2e6`, `background: #fff`, `color: #495057` | border/raised/secondary |
| `.toggle-btn.active` `border-color: #0066cc`, `background: #e8f0fb`, `color: #0066cc` | primary/subtle |
| `.toggle-btn:hover:not(.active)` `border-color: #adb5bd`, `background: #f8f9fa` | emphasis/surface |
| `.radio-label:has(input:checked)` `background: #e8f0fb`, `border-color: #0066cc` | subtle/primary |
| `.radio-hint` `color: #6c757d` | muted |
| `.form-select border: 2px solid #dee2e6`, `background: #fff` | border/raised |
| `.form-select:focus border-color: #0066cc` | primary |
| `.delta-label`, `.macro-split-field label` `color: #495057` | secondary |
| `.delta-unit` `color: #6c757d` | muted |
| `.macro-sum-indicator.sum-valid` `background: #d4edda`, `color: #155724` | success-bg/text |
| `.macro-sum-indicator.sum-invalid` `background: #f8d7da`, `color: #721c24` | danger-bg/dark |
| `.field-hint`, `.settings-hint` `color: #6c757d` | muted |
| `.form-control border: 2px solid #dee2e6` | border |
| `.form-control:focus border-color: #0066cc` | primary |
| `.modal-footer-bar border-top: 1px solid #e9ecef` | border |
| `.btn-primary` `background: #0066cc` | primary |
| `.btn-primary:hover` `background: #0052a3` | primary-hover |
| `.btn-secondary` `background: #f0f0f0`, `color: #333` | subtle/body |
| `.btn-secondary:hover` `background: #e0e0e0` | hover |

#### `UsdaSearchDialog.razor.css`
| Hardcoded | Token |
|---|---|
| `.modal-content-box` `background: #fff` | raised |
| `.modal-header-bar border-bottom: 1px solid #e9ecef` | border |
| `.modal-title-text` `color: #1a1a1a` | heading |
| `.modal-close-btn` `color: #6c757d` | muted |
| `.modal-close-btn:hover` `color: #212529`, `background: #f0f0f0` | body/subtle |
| `.results-table-wrapper border: 1px solid #dee2e6` | border |
| `.page-info` `color: #495057` | secondary |
| `.modal-footer-bar border-top: 1px solid #e9ecef` | border |

#### `SeasonalityPanel.razor.css`
| Hardcoded | Token |
|---|---|
| `.seasonality-panel` `background: #f8f9fa`, `border: 1px solid #dee2e6` | surface/border |
| `.seasonality-header` `background: #ffffff`, `border-bottom: 2px solid #dee2e6` | raised/border |
| `.seasonality-header h2` `color: #495057` | secondary |
| `.seasonality-loading`, `.seasonality-unavailable`, `.seasonality-best` `color: #6c757d` | muted |
| `.seasonality-score` `color: #2e7d32` | success-emphasis |
| `.seasonality-table` `background: white` | raised |
| `.seasonality-table thead th` `background: #f8f9fa`, `color: #495057`, `border-bottom: 2px solid #dee2e6` | surface/secondary/border |
| `.seasonality-table td border-bottom: 1px solid #dee2e6` | border |
| `.col-name` `color: #1a1a1a` | heading |
| `.row-in-season` `background: #ffffff` | raised |
| `.row-in-season:hover` `background: #f0f8f0` | success-bg-subtle |
| `.row-out-season` `background: #fff8e1` | warning-bg-subtle |
| `.row-out-season:hover` `background: #fff3cd` | warning-bg |
| `.badge-in` `background: #28a745` | success |
| `.badge-out` `background: #dc3545` | danger |
| `.scarcity-high` `background: #fce4ec`, `color: #880e4f` | danger-bg / danger-dark |
| `.scarcity-medium` `background: #fff3e0`, `color: #e65100` | warning-bg / warning-text |
| `.scarcity-low` `background: #e8f5e9`, `color: #2e7d32` | success-bg-subtle / success-emphasis |
| `.row-callout` `background: #fff3cd` | warning-bg |
| `.callout-cell` `color: #856404` | warning-text |

#### `SeasonalityBadge.razor.css`
| Hardcoded | Token |
|---|---|
| `.seasonality-badge--peak` `background: #e8f5e9`, `color: #2e7d32` | success-bg-subtle/emphasis |
| `.seasonality-badge--partial` `background: #fff8e1`, `color: #f57f17` | warning-bg-subtle/warning-text |
| `.seasonality-badge--off` `background: #eceff1`, `color: #546e7a` | surface/muted (close) |
| `.seasonality-badge--unavailable` `background: #f5f5f5`, `color: #9e9e9e` | subtle/placeholder |

#### `ParsedIngredientsTable.razor.css`
| Hardcoded | Token |
|---|---|
| `.parsed-ingredients-container` `background: #f8f9fa`, `border: 1px solid #dee2e6` | surface/border |
| `.nutrition-header-pinned` `background: #ffffff`, `border-bottom: 2px solid #dee2e6` | raised/border |
| `.nutrition-label` `color: #495057` | secondary |
| `.nutrition-name` `color: #6c757d` | muted |
| `.nutrition-value` `color: #2e7d32` | success-emphasis |
| `.loading-spinner` `color: #6c757d` | muted |
| `.no-data` `color: #6c757d` | muted |
| `.parsed-ingredients-table` `background: white` | raised |
| `.parsed-ingredients-table thead` `background: #f8f9fa`, `border-bottom: 2px solid #dee2e6` | surface/border |
| `.parsed-ingredients-table th` `color: #495057`, `background: #f8f9fa` | secondary/surface |
| `.parsed-ingredients-table td border-bottom: 1px solid #dee2e6` | border |
| `.row-matched` `background: #ffffff` | raised |
| `.row-matched:hover` `background: #f0f8f0` | success-bg-subtle |
| `.row-unmatched` `background: #fff8e1` | warning-bg-subtle |
| `.row-unmatched:hover` `background: #ffe082` | warning-bg (slightly stronger, keep as var) |
| `.badge-success` `background: #28a745` | success |
| `.badge-warning` `background: #ff9800` | warning (close to `--color-warning`) |
| `.match-distance` `color: #ff9800` | warning |
| `.matched-food-name` `color: #1a1a1a` | heading |
| `.unmatched-text` `color: #d32f2f` | danger |
| `.text-muted` `color: #6c757d` | muted |

---

## File Change Summary

| File | Type | Action |
|---|---|---|
| `Broccoli.App.Shared/Services/IThemeService.cs` | C# interface | **Create** |
| `Broccoli.App.Shared/Services/ThemeService.cs` | C# service | **Create** |
| `Broccoli.App.Shared/wwwroot/js/themeInterop.js` | JavaScript | **Create** |
| `Broccoli.App.Shared/Components/AppSettingsDialog.razor` | Razor | **Create** |
| `Broccoli.App.Shared/Components/AppSettingsDialog.razor.cs` | C# code-behind | **Create** |
| `Broccoli.App.Shared/Components/AppSettingsDialog.razor.css` | CSS | **Create** |
| `Broccoli.App.Shared/Layout/MainLayout.razor` | Razor | **Modify** |
| `Broccoli.App.Shared/Layout/MainLayout.razor.css` | CSS | **Modify** |
| `Broccoli.App.Web/Components/App.razor` | Razor | **Modify** (add script tag) |
| `Broccoli.App/wwwroot/index.html` | HTML | **Modify** (add script tag) |
| `Broccoli.App.Web/Program.cs` | C# | **Modify** (DI registration) |
| `Broccoli.App/MauiProgram.cs` | C# | **Modify** (DI registration) |
| `Broccoli.App.Shared/wwwroot/app.css` | CSS | **Modify** (tokens + migrations) |
| `Broccoli.App.Shared/Pages/Login.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/Recipes.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/RecipeDetail.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/RecipeReadOnly.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/Foods.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/GroceryList.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/Pantry.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/MacroTargets.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/MealPrepPlans.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Pages/DailyFoodPlanning.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/AddIngredientsDialog.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/AddRecipesToPlanDialog.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/MacroTargetSettingsDialog.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/UsdaSearchDialog.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/SeasonalityPanel.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/SeasonalityBadge.razor.css` | CSS | **Modify** |
| `Broccoli.App.Shared/Components/ParsedIngredientsTable.razor.css` | CSS | **Modify** |

**Total: 6 new files, 23 modified files**

---

## Out of Scope

- Bootstrap component colours (`.table`, `.btn`, etc.) — Bootstrap ships its own CSS variables (`--bs-*`) from v5.2+; the app uses a custom `.btn-primary` override in `app.css` which will be tokenized. Full Bootstrap dark-mode support would require adding `data-bs-theme="dark"` to `<html>` alongside `data-theme="dark"` — can be added as a follow-up one-liner in `themeInterop.js` if needed.
- `Broccoli.App.Web/Components/Layout/ReconnectModal.razor.css` — framework reconnect UI, no user-facing colours to migrate.
- Unit / integration tests — `ThemeService` is straightforward to unit-test (mock `ISecureStorageService`); not required for this feature to ship.

