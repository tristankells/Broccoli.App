# Feature Plan: Collapsible Navigation & Screen Wake Lock

---

## Feature 1 — Collapsible Navigation on Desktop

### Overview

On screens ≥ 641 px the sidebar is always shown at 250 px. Add a collapse toggle so the user can shrink the sidebar to an icon-only rail (~60 px) or restore it at any time. The chosen state persists across page loads via `localStorage`.

### Current Layout (for reference)

```
MainLayout.razor
└── <div class="page">
    ├── <div class="sidebar">   ← 250 px on desktop, full-height sticky
    │   └── <NavMenu />
    └── <main>
        ├── <div class="top-row">
        └── <article>  @Body
```

The sidebar width and the main content flex share are defined in `MainLayout.razor.css`; the nav drawer uses a pure-CSS checkbox toggle in `NavMenu.razor.css` that only operates on mobile.

---

### Architecture

#### New shared service — `INavStateService`

Create `Layout/NavStateService.cs` + `INavStateService.cs` in `Broccoli.App.Shared.Layout`:

```csharp
public interface INavStateService
{
    bool IsCollapsed { get; }
    event Action? OnChanged;
    void Toggle();
    Task InitializeAsync();   // loads persisted value from localStorage
}
```

Register as **scoped** in both host projects (DI wiring in `Program.cs` / `MauiProgram.cs`), because layout state belongs to a single browser session.  Uses `IJSRuntime` to read/write `localStorage.getItem("navCollapsed")` for persistence.

#### `MainLayout.razor` changes

- Inject `INavStateService`
- Subscribe to `OnChanged` and call `StateHasChanged()`
- Apply a CSS class to `.page`: `<div class="page @(NavState.IsCollapsed ? "nav-collapsed" : "")">`
- Call `NavState.InitializeAsync()` in `OnAfterRenderAsync` (first render) to restore saved state

#### `NavMenu.razor` changes (desktop only)

- Inject `INavStateService`
- Add a `<button class="nav-collapse-btn">` at the bottom of the nav, **visible only on desktop** (hidden via CSS on mobile)
- Button label: `«` when expanded, `»` when collapsed
- On click: `NavState.Toggle()`

#### Collapsed state — icon-only rail

When collapsed the sidebar shrinks to 60 px and only the emoji icon of each nav link is visible (text hidden with `overflow: hidden` / `width: 0` on the text span). A tooltip (`title` attribute) on each nav link preserves discoverability.

---

### Files Changed / Created

| File | Change |
|---|---|
| `Layout/INavStateService.cs` *(new)* | Interface |
| `Layout/NavStateService.cs` *(new)* | Implementation using `IJSRuntime` for localStorage |
| `Layout/NavMenu.razor` | Add collapse button, subscribe to service |
| `Layout/NavMenu.razor.css` | Collapse button styles; icon-rail styles at ≥641 px; add text-span to each nav link item |
| `Layout/MainLayout.razor` | Inject service, apply `nav-collapsed` class to `.page` |
| `Layout/MainLayout.razor.css` | `.page.nav-collapsed .sidebar { width: 60px; }` and flex adjustments |
| `Broccoli.App/MauiProgram.cs` | Register `INavStateService` |
| `Broccoli.App.Web/Program.cs` | Register `INavStateService` |

---

### Behaviour Details

| State | Sidebar width | Nav link text | Toggle button |
|---|---|---|---|
| Expanded (default) | 250 px | Visible | `«` (collapse) |
| Collapsed | 60 px | Hidden | `»` (expand) |

- On **mobile** (< 641 px): the collapse toggle is hidden; existing burger-menu behaviour is unchanged
- On **desktop**: the mobile burger icon is already hidden; the new collapse button appears at the bottom of the drawer
- The `nav-collapsed` class on `.page` is also used to remove the min-width constraint from `<main>` so content fills the extra space

---

## Feature 2 — Prevent Screen Dimming on the Recipe Read-Only Page

### Overview

Add an opt-in setting to the Read-Only recipe page that acquires a **Screen Wake Lock** while the user is reading, preventing the device screen from dimming or locking. The setting is toggled via a small settings dialog on that page and persisted to `localStorage` (device-scoped preference, not per-user Cosmos data).

---

### Platform support

| Platform | Mechanism |
|---|---|
| Web (modern browsers) | [Screen Wake Lock API](https://developer.mozilla.org/en-US/docs/Web/API/Screen_Wake_Lock_API): `navigator.wakeLock.request('screen')` |
| MAUI Android / iOS / Mac | `DeviceDisplay.Current.KeepScreenOn = true` (via `Microsoft.Maui.Devices`) |
| MAUI Windows | Same `DeviceDisplay` API |

The Web host handles this entirely via JS interop. The MAUI host uses the `IWakeLockService` platform abstraction (see below).

---

### Architecture

#### Platform abstraction — `IWakeLockService`

Add to `_Shared/Platform/`:

```csharp
public interface IWakeLockService
{
    Task AcquireAsync();
    Task ReleaseAsync();
}
```

- **Web implementation** (`Broccoli.App.Web/Services/WakeLockService.cs`): calls JS interop (`wakeLock.acquire()` / `wakeLock.release()`)
- **MAUI implementation** (`Broccoli.App/Services/WakeLockService.cs`): sets `DeviceDisplay.Current.KeepScreenOn`

#### New JS interop — `wakeLock.js`

Add to `Broccoli.App.Web/wwwroot/`:

```js
window.wakeLock = {
    _sentinel: null,
    async acquire() {
        if (!('wakeLock' in navigator)) return;
        try {
            this._sentinel = await navigator.wakeLock.request('screen');
            // Re-acquire if the browser releases it on tab hide/show
            document.addEventListener('visibilitychange', async () => {
                if (document.visibilityState === 'visible' && this._sentinel?.released) {
                    this._sentinel = await navigator.wakeLock.request('screen');
                }
            }, { once: true });
        } catch { /* permission denied or not supported — silently ignore */ }
    },
    async release() {
        await this._sentinel?.release();
        this._sentinel = null;
    }
};
```

#### Setting persistence — `localStorage`

The preference (`keepScreenOn: true/false`) is stored in `localStorage` under key `"recipeReadOnlyKeepScreenOn"` via JS interop. This is a device-level preference (you may want wake lock on your phone but not on your laptop) so `localStorage` is more appropriate than the per-user Cosmos document.

#### New settings dialog — `RecipeReadOnlySettingsDialog.razor`

Placed in `Slices/Recipes/`, following the same structural pattern as `RecipeDetailSettingsDialog.razor`. Contains a single On/Off toggle for "Keep screen on while reading". Emits `OnSave(bool keepScreenOn)` and `OnCancel`.

#### `RecipeReadOnly.razor` + `RecipeReadOnly.razor.cs` changes

**New state / injections:**
```csharp
[Inject] IWakeLockService WakeLockService { get; set; }
[Inject] IJSRuntime JSRuntime { get; set; }

private bool _keepScreenOn;
private bool _settingsDialogOpen;
```

**Lifecycle:**
- `OnAfterRenderAsync(firstRender)`: load `_keepScreenOn` from `localStorage` via JS interop; if true, call `WakeLockService.AcquireAsync()`
- `IAsyncDisposable.DisposeAsync()`: call `WakeLockService.ReleaseAsync()` unconditionally (safe no-op when not held)

**Settings save handler:**
```csharp
private async Task OnSettingsSaved(bool keepScreenOn)
{
    _keepScreenOn = keepScreenOn;
    await JSRuntime.InvokeVoidAsync("localStorage.setItem",
        "recipeReadOnlyKeepScreenOn", keepScreenOn.ToString().ToLower());

    if (keepScreenOn) await WakeLockService.AcquireAsync();
    else              await WakeLockService.ReleaseAsync();
}
```

**Markup additions:**
- ⚙️ Settings button in `.read-header-actions`
- `<RecipeReadOnlySettingsDialog>` before closing `</AuthorizeView>`

---

### Files Changed / Created

| File | Change |
|---|---|
| `_Shared/Platform/IWakeLockService.cs` *(new)* | Interface |
| `Broccoli.App.Web/Services/WakeLockService.cs` *(new)* | JS interop implementation |
| `Broccoli.App.Web/wwwroot/wakeLock.js` *(new)* | JS helper (acquire / release / visibility re-acquire) |
| `Broccoli.App.Web/Components/App.razor` or host page | Add `<script src="wakeLock.js">` |
| `Broccoli.App/Services/WakeLockService.cs` *(new)* | MAUI implementation via `DeviceDisplay` |
| `Broccoli.App.Web/Program.cs` | Register `IWakeLockService` |
| `Broccoli.App/MauiProgram.cs` | Register `IWakeLockService` |
| `Slices/Recipes/RecipeReadOnlySettingsDialog.razor` *(new)* | On/Off toggle dialog |
| `Slices/Recipes/RecipeReadOnlySettingsDialog.razor.css` *(new)* | Modal styles (reuse same pattern) |
| `Slices/Recipes/RecipeReadOnly.razor` | Settings button, dialog, inject new services |
| `Slices/Recipes/RecipeReadOnly.razor.cs` | Wake lock lifecycle, settings state, `IAsyncDisposable` |
| `Slices/Recipes/RecipeReadOnly.razor.css` | Settings button style |

---

### Edge Cases

| Scenario | Handling |
|---|---|
| Browser doesn't support Wake Lock API | JS helper guards with `if (!('wakeLock' in navigator))` — silently no-ops |
| User navigates away mid-read | `DisposeAsync` releases the lock |
| Tab is hidden (browser releases lock automatically) | `visibilitychange` listener re-acquires when tab becomes visible again |
| MAUI — app backgrounded | MAUI OS lifecycle will handle this; `KeepScreenOn` is reset on foreground if needed |
| User toggles setting off during reading | `ReleaseAsync()` called immediately |
| Setting persists across app restarts | `localStorage` is read on `OnAfterRenderAsync` first render |

