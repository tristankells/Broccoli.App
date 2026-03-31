# Broccoli App — UI Design Reference

Use this document when styling new or existing components to stay consistent with the established design language.

---

## Brand Identity

- **App name:** Broccoli
- **Personality:** Fresh, natural, food-focused. Clean but warm — not clinical.
- **Logo:** SVG broccoli icon (white floret) + "Broccoli" wordmark. Lives at `_content/Broccoli.App.Shared/broccoli-logo.svg`.
- **Favicon:** Broccoli icon on green rounded square. Lives at `_content/Broccoli.App.Shared/favicon.svg`.

---

## Color Palette

All colors are CSS custom properties defined in `Broccoli.App.Shared/wwwroot/app.css`. Always use these tokens — never hardcode hex values in components.

### Primary (Brand Green)

| Token | Light | Dark |
|---|---|---|
| `--color-primary` | `#2e7d32` (forest green) | `#66bb6a` (sage green) |
| `--color-primary-hover` | `#1b5e20` | `#81c784` |
| `--color-primary-dark` | `#145214` | `#43a047` |
| `--color-primary-subtle` | `#e8f5e9` | `#1a3d1f` |
| `--color-primary-wash` | `#f1f8f1` | `#0f2a12` |

### Backgrounds

| Token | Purpose |
|---|---|
| `--color-bg-page` | Page/body background |
| `--color-bg-surface` | Cards, panels, sidebars |
| `--color-bg-raised` | Modals, dropdowns (elevated) |
| `--color-bg-subtle` | Zebra rows, subtle fills |
| `--color-bg-hover` | Hover states on rows/buttons |
| `--color-bg-selected` | Selected/active row background (green-tinted) |

### Text

| Token | Use for |
|---|---|
| `--color-text-heading` | Page titles, dialog headers |
| `--color-text-body` | Default body copy |
| `--color-text-secondary` | Labels, secondary info |
| `--color-text-muted` | Helper text, timestamps, placeholders |

### Semantic

| Purpose | Tokens |
|---|---|
| Success | `--color-success`, `--color-success-bg`, `--color-success-text` |
| Warning | `--color-warning`, `--color-warning-bg`, `--color-warning-text` |
| Danger | `--color-danger`, `--color-danger-bg`, `--color-danger-dark` |
| Borders | `--color-border`, `--color-border-input`, `--color-border-subtle` |

---

## Chrome & Layout

### Sidebar / Navigation

- **Gradient:** `linear-gradient(180deg, #1b4332 0%, #2d6a4f 100%)` — deep forest green top to mid green
- Nav link text: `#d7d7d7`, active: `rgba(255,255,255,0.37)` background, hover: `rgba(255,255,255,0.1)` background
- Nav icon size: `1.1rem × 1.1rem`, `margin-right: 0.6rem`
- Collapse rail width: `60px` (icon-only), expanded: `250px`

### Login Page

- **Background:** `linear-gradient(150deg, #1b4332 0%, #2d6a4f 55%, #52b788 100%)`
- Logo above card: `<img src="_content/Broccoli.App.Shared/broccoli-logo.svg" />` at `width: 200px`
- Tagline: *"Your kitchen, planned."* — `rgba(255,255,255,0.8)`, `1rem`, centered
- Card: `max-width: 450px`, `border-radius: 12px`, `box-shadow: 0 10px 40px rgba(0,0,0,0.2)`

---

## Icons

**Always use inline Lucide SVGs** — no emoji, no image files, no icon font.

### Standard attributes on every icon

```html
<svg viewBox="0 0 24 24" fill="none" stroke="currentColor"
     stroke-width="2" stroke-linecap="round" stroke-linejoin="round"
     aria-hidden="true">
```

Sizing is set by CSS class, not inline `width`/`height`. The `stroke="currentColor"` means icons automatically inherit the parent element's text colour, including hover/active states.

### Nav icon sizes

```css
.nav-icon {
    width: 1.1rem;
    height: 1.1rem;
    flex-shrink: 0;
    margin-right: 0.6rem;
}
```

### General UI icon sizes

| Context | Size |
|---|---|
| Inline with text | `1rem × 1rem` |
| Button icon (toolbar) | `1.1–1.25rem` |
| Empty-state / illustration | `2–3rem` |

### Icon → Lucide path mapping

| Screen | Icon name | SVG path |
|---|---|---|
| Recipes | `utensils` | `<path d="M3 2v7c0 1.1.9 2 2 2h4a2 2 0 0 0 2-2V2"/><line x1="7" y1="2" x2="7" y2="22"/><path d="M21 15V2a5 5 0 0 0-5 5v6c0 1.1.9 2 2 2h1v5a2 2 0 0 0 4 0z"/>` |
| Meal Prep | `calendar-days` | `<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/><path d="M8 14h.01"/><path d="M12 14h.01"/><path d="M16 14h.01"/><path d="M8 18h.01"/><path d="M12 18h.01"/>` |
| Daily Food Planning | `calendar` | `<rect x="3" y="4" width="18" height="18" rx="2" ry="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/>` |
| Food Database | `leaf` | `<path d="M11 20A7 7 0 0 1 9.8 6.1C15.5 5 17 4.48 19 2c1 2 2 4.18 2 8 0 5.5-4.78 10-10 10z"/><path d="M2 21c0-3 1.85-5.36 5.08-6C9.5 14.52 12 13 13 12"/>` |
| Grocery List | `shopping-cart` | `<circle cx="8" cy="21" r="1"/><circle cx="19" cy="21" r="1"/><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"/>` |
| Pantry | `archive` | `<rect x="2" y="3" width="20" height="5" rx="1"/><path d="M4 8v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8"/><path d="M10 12h4"/>` |
| Macro Targets | `target` | `<circle cx="12" cy="12" r="10"/><circle cx="12" cy="12" r="6"/><circle cx="12" cy="12" r="2"/>` |
| Settings | `settings` | `<path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/>` |
| Add / Plus | `plus` | `<line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>` |
| Delete / Trash | `trash-2` | `<polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/><line x1="10" y1="11" x2="10" y2="17"/><line x1="14" y1="11" x2="14" y2="17"/>` |
| Edit / Pencil | `pencil` | `<path d="M17 3a2.85 2.83 0 1 1 4 4L7.5 20.5 2 22l1.5-5.5Z"/>` |
| Search | `search` | `<circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>` |
| Close / X | `x` | `<line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>` |
| Check | `check` | `<polyline points="20 6 9 12 4 10"/>` (or `<path d="M20 6 9 17l-5-5"/>`) |
| Info | `info` | `<circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/>` |
| Warning | `alert-triangle` | `<path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/>` |
| Import | `upload` | `<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="17 8 12 3 7 8"/><line x1="12" y1="3" x2="12" y2="15"/>` |
| Download / Export | `download` | `<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>` |

---

## Component Patterns

### Buttons

Use Bootstrap classes. The primary colour is inherited from `--color-primary` via Bootstrap's `--bs-primary` override (wired via app.css).

```html
<button class="btn btn-primary">Save</button>
<button class="btn btn-secondary">Cancel</button>
<button class="btn btn-danger">Delete</button>
<button class="btn btn-outline-secondary">Edit</button>
```

For icon buttons, pair an inline SVG with a text label:

```html
<button class="btn btn-primary">
    <svg class="btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor"
         stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
    </svg>
    Add Item
</button>
```

```css
.btn-icon {
    width: 1rem;
    height: 1rem;
    margin-right: 0.35rem;
    vertical-align: -0.125em; /* optical alignment */
}
```

### Modals / Dialogs

Follow the pattern established in `AddIngredientsDialog.razor`:

```html
<div class="modal-backdrop" @onclick="Cancel"></div>
<div class="modal-dialog-container" role="dialog" aria-modal="true" aria-labelledby="dialog-title">
    <div class="modal-content-box">
        <div class="modal-header-bar">
            <h2 id="dialog-title" class="modal-title-text">Title</h2>
            <button class="modal-close-btn" @onclick="Cancel" title="Close">&times;</button>
        </div>
        <div class="modal-body-scroll">
            <!-- content -->
        </div>
        <div class="modal-footer-bar">
            <button class="btn btn-secondary" @onclick="Cancel">Cancel</button>
            <button class="btn btn-primary" @onclick="Confirm">Confirm</button>
        </div>
    </div>
</div>
```

- Header title: no emoji — use a Lucide SVG before the text if an icon is desired
- Footer: Cancel on the left, primary action on the right
- Accessible: always include `role="dialog"`, `aria-modal="true"`, `aria-labelledby` pointing to the heading

### Cards / Panels

```css
border-radius: 8px;                          /* standard — use 12px for hero cards */
background-color: var(--color-bg-raised);
border: 1px solid var(--color-border);
box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);  /* subtle shadow */
```

### Form Controls

Bootstrap `.form-control` / `.form-select` are used throughout. They automatically adapt to dark mode via the `--bs-*` variable overrides in `app.css`.

Labels use `--color-text-secondary` (`font-weight: 500`). Validation messages inherit Bootstrap's danger colour.

---

## Dark Mode

Dark mode is toggled by setting `data-theme="dark"` on the `<html>` element (managed by `ThemeService`). All component styles must use CSS custom property tokens — **never hardcode colours** — so that dark mode overrides apply automatically.

The dark mode primary green is lighter (`#66bb6a`) so it reads against dark backgrounds at sufficient contrast.

---

## MAUI-Specific Notes

- App icon (`Resources/AppIcon/appicon.svg`): green rounded square (`#2e7d32`) + white broccoli silhouette
- Splash screen (`Resources/Splash/splash.svg`): mid-green background (`#2d6a4f`) + broccoli icon + "Broccoli" wordmark
- Status-bar safe-area strip: `rgb(27, 67, 50)` — matches the top of the sidebar gradient
