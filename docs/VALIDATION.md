# Validation — 0.1.0 preview

## Verified on development Windows 11 host

- .NET Framework compiler builds the app and offline installer; all 17 automated assertions pass.
- Cloth: dimensions, corner tracking, bounded stretch, floor resistance, retained fold, settling, translation, flattening, long-frame stability, and upper-layer clearance of 3.5 pixels in the fold regression.
- Theme logic: selected theme + expected picture enable; another theme, changed picture, slideshow, solid color, or missing state disable. Center plain-drag moves; all four edges fold without modifiers.
- Native WPF flat and folded renders inspected. Preview mouse folding was exercised.
- Installer upgrade installs a discoverable named MLM Desktop tile without requiring the user to save a theme manually.
- Selecting that tile in Windows Settings yields the canonical installed theme path and active wallpaper match.
- Active process has one visible overlay with WS_EX_TOOLWINDOW, WS_EX_NOACTIVATE, WS_EX_LAYERED and no WS_EX_APPWINDOW.
- Selecting Windows (dark) in Settings disables the theme match and closes the overlay (zero visible app windows).
- No MLM Desktop shortcuts found in the user's desktop or Start menu. No notification icon code exists.
- Setup payload extraction is checked against the five app files by SHA-256 before release.

## Remaining manual acceptance

- Full native desktop interaction: select multiple actual icons, place them on uncovered floor, cover and reveal them; transparent-area click-through and context menu.
- Many rapid, repeated folds, multiple fold directions, and corner cases beyond the settled overlap regression.
- Show Desktop, Explorer restart, mixed DPI, multi-monitor changes, and Windows 10.
- Complete uninstall round trip; in-use wallpaper preservation is implemented but not exercised by deleting this development installation.

The 0.1.0 release remains a preview. The passing automated and theme lifecycle checks do not certify the remaining desktop acceptance matrix.

## 0.1.1 release verification

- Rebuilt user changes adding scheduled-task recovery for the installed theme watcher.
- All 17 existing cloth and theme/input assertions passed.
- All five installer payload files matched build output by SHA-256 after extraction.
- Setup version and package filenames updated to 0.1.1.
- Scheduled-task registration, restart recovery, and uninstall were not exercised in this release run. Earlier native theme lifecycle checks above apply to 0.1.0.
- Published as a regular release at the owner's request; the known acceptance gaps above remain.
