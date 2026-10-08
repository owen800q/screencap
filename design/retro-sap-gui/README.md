# Retro SAP GUI Design System

A pixel-faithful recreation of the classic SAP GUI desktop look — the beveled, 3D, MS-Sans-Serif aesthetic that defined enterprise workstations from the late 90s through the 2000s. This system is for prototyping retro-feel desktop interfaces, terminal/back-office tools, "ironic enterprise" software, retro-tech promo work, and any UI that wants to feel like it lives inside a Windows 9x SAPGUI session.

## Source

- `uploads/compressed_Frame 1.png` — single reference image showing a comprehensive design-system board labeled **"DESIGN SYSTEM — RETRO SAP GUI STYLE"**. Contains a full inventory of components: color palette, typography, borders, icons, application commands, buttons, links, radio/checkbox, inputs, selects, cascaders, switches, upload, sliders, tables, tabs, tags, descriptions, skeletons, empty states, results, breadcrumbs, page headers, dropdowns, dialogs, tooltips, popconfirms, cards, carousels, collapses, timelines, dividers, calendars, images, backtops, infinite scroll, avatars, drawers, pagination, loading, message boxes, progress, badges, messages.

No codebase, no Figma, no font files were attached. This system was built from the reference image plus knowledge of classic SAP GUI / Windows 9x conventions.

## Index

- `README.md` — this file (start here)
- `SKILL.md` — Claude Code-compatible skill manifest
- `colors_and_type.css` — design tokens: colors, type stacks, scale, spacing, bevel composites, gradients
- `components.css` — component classes (`.sap-btn`, `.sap-input`, `.sap-tabs`, `.sap-table`, `.sap-dialog`, etc.) — companion to the JSX components
- `assets/`
  - `icons.svg` — 40+ pixel-style 16×16 SAP-flavored icons (use as `<svg><use href="…/icons.svg#NAME"/></svg>`)
  - `icons.html` — visual sprite reference
  - `logo.svg` — design-system wordmark (NOT the real SAP logo)
- `preview/` — small Design System tab cards (one HTML file per token / component group)
- `ui_kits/sap_gui/` — interactive React recreation of a Shipment Maintenance transaction
  - `index.html` — the working prototype
  - `Window.jsx`, `Toolbar.jsx`, `Form.jsx`, `DataTable.jsx`, `TabStrip.jsx`, `Dialog.jsx`, `controls.jsx`

## Content fundamentals

SAP GUI copy is **terse, system-voice, and command-oriented**. It speaks at the user, not with them. Tone is functional, slightly bureaucratic, never warm.

- **Voice:** third-person system / imperative ("Save", "Execute", "Cancel"). Never "I", rarely "you" — the system addresses the operator, not a user.
- **Casing:** Title Case for menu items and button labels ("Track Shipment", "Table Settings"). UPPERCASE acronyms preserved (SAP, OK, ID).
- **Status messages:** clipped, factual. "Document saved.", "No data available.", "Field is required.", "Loading…"
- **Numbers and IDs everywhere:** customer numbers, document IDs, transaction codes (e.g. `VL01N`, `ME21N`). Never humanized — `Customer Number 6000068250`, not "Acme Corp".
- **No emoji. Ever.** This is a transaction-processing UI, not a consumer app.
- **No marketing flourish.** No exclamation points, no "Awesome!", no encouragement. Errors say "Error", successes say "Success", that's it.
- **Field labels** are short nouns, often ending with a colon: `Carrier:`, `Service:`, `Payment:`. Help text sits below the field, gray, one short sentence.
- **Confirmation dialogs** ask a flat question: "Delete this record?" with `OK` / `Cancel`.

Examples from the reference: `Execute`, `Back`, `Track Shipment`, `Table Settings`, `Drag & drop area`, `No Data Available`, `Customer Number 3250`, `Statistic 14.55.1293`, `Home > Shipment > Create`.

## Visual foundations

### Color
A cool, desaturated palette dominated by **steel blues** and **silver grays**, punctuated by a single warm **SAP amber/gold** for selection, focus, and primary highlight.

- **Backgrounds:** `--surface` (#D6E4F1, the iconic SAP wash), `--surface-2` (#EAF1F8 panel), `--surface-3` (#F5F8FB inset)
- **Header/title bar:** deep blue gradient `#003D7A → #4A7FB8` with a glossy highlight band
- **Accent (selection, focus, active row, primary button):** `--accent` #F0AB00, `--accent-soft` #FFE8A8
- **Borders:** `--border-light` #FFFFFF (top/left bevel), `--border-dark` #6E7A89 (bottom/right bevel), `--border-flat` #A6B4C5
- **Text:** `--fg` #000000, `--fg-muted` #4A5566, `--fg-disabled` #8A95A5
- **Status:** error #C8281E, warning #E8A100, info #1F6BB8, success #2E8B3E
- **Link:** unvisited #1F6BB8, visited #6B3F8A, hover red-orange underline

### Typography
- **Body:** "Microsoft Sans Serif" (bundled in `fonts/`), Tahoma fallback, at **11–12px**. Slight letter-spacing 0, no anti-alias smoothing where possible.
- **Bold headings / table headers:** same family at 700 weight, 12–14px.
- **Italic:** used sparingly, only for placeholders and the rare "(optional)" annotation.
- **Numerics:** tabular-nums for tables, IDs, calendars.
- The real bitmap font is loaded via `@font-face` at the top of `colors_and_type.css` — no further setup needed.

> ✅ **Font bundled.** `fonts/MicrosoftSansSerif.ttf` ships with the system. If you want pixel-perfect raster rendering at every zoom (instead of the slight smoothing TTF gets), drop in a pixel-traced WOFF2 like `W95FA` alongside it and add it to the `--font-system` stack.

### Borders, bevels, shadows
This is a **3D-bevel** system. Every panel, button, input, and tab has a sharp two-tone border.

- **Outset bevel (raised, e.g. button at rest):** 1px white top+left, 1px dark gray (#6E7A89) bottom+right, optional outer 1px black hairline.
- **Inset bevel (pressed, input field, selected tab):** inverted — dark top+left, white bottom+right.
- **Beveled panel:** double-line — outer light, inner dark, separated by 1px filler. Used for grouping (`Form`, `Cards` regions in reference).
- **No drop shadows.** No blur. The "depth" is entirely from 1-pixel bevels and color blocks.
- **Corner radius: 0.** Everything is square. The single exception is the avatar circle.

### Spacing
Tight. Enterprise-density. 4px grid, but 2px and 1px are common.
`--space-1: 2px; --space-2: 4px; --space-3: 6px; --space-4: 8px; --space-5: 12px; --space-6: 16px;`

### Backgrounds
- The signature **light-blue desktop wash** (#D6E4F1) covers the canvas.
- **Title bars** use a horizontal 3-stop gradient — a glossy specular highlight near the top emulates the "Windows XP / SAP NetWeaver" look.
- **Toolbars** use a vertical pale gradient (#F5F8FB → #DCE6F2).
- **Selected table row** is solid amber (#F0AB00) with black text.
- No textures, no patterns, no images-as-decor. The chrome is the decor.

### Animation
- **Almost none.** Buttons flip bevel on press (instant). Hover changes color or shows an underline (instant). No fades, no easing curves, no spring.
- The only motion is **indeterminate loading** (a small barber-pole or rotating dots) and the carousel slide.
- If easing is ever needed: `linear` or `step-end`. Never `ease-out`, never `cubic-bezier`.

### Hover / press / focus / disabled
- **Hover (button):** background lightens to `--accent-soft` (#FFE8A8) or fill swaps to amber for primary affordances. Cursor → `default` for buttons (yes, really — SAP convention), `text` for inputs.
- **Hover (link):** color shifts from blue to red-orange (`#C8281E`), underline appears.
- **Press (button):** bevel inverts (inset look), background darkens 1 step. No transform, no scale.
- **Focus:** dotted 1px black outline, inset 1px from the control edge — the classic Windows focus ring.
- **Disabled:** desaturated, text → `--fg-disabled` (#8A95A5), background → `--surface-2`, no bevel change on hover.
- **Selected row:** solid amber background + black text, persists.

### Cards / panels / layout
- A "card" here is a **beveled panel** — a flat fill with the two-tone bevel border, optional 1px black outer hairline, and a small bold title floating on the top-left of the border (the classic `<fieldset><legend>` look). See `Form > Cards > Payment` in the reference.
- Layout is **dense, grid-aligned, label-on-left**. Forms use a 2-column structure: label right-aligned, control left-aligned, both on the same row.
- **Fixed elements:** menu bar pinned top, toolbar below it, status bar pinned bottom. No sticky scroll, no parallax.
- **Transparency / blur:** none. Everything is fully opaque.

### Iconography
- **16×16 pixel-art icons**, full color, hard edges. Document, save, print, search, refresh, tools, gears, charts.
- Icons live in toolbars and inline with menu items. Never decorative.
- **No emoji, no Unicode pictographs.** ✓ and ✗ are rendered as small pixel marks, not Unicode.
- See `assets/icons.html` for the icon sprite reference.

## ui_kits

- `ui_kits/sap_gui/` — a transactional desktop screen mocking up a "Shipment Maintenance" view, demonstrating menu bar, toolbar, tabs, beveled forms, table with selection, status bar, and dialog overlays.
