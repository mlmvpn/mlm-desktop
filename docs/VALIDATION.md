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

## 0.2.0 candidate verification — 2026-10-14

- All eight optimized x64 suites passed: Cloth, Physics, Bending, Grip, Stress, Roll, Contact and DesktopMode.
- Final source reviewed independently; remaining throughput limitation is documented in physics-baseline.md.
- Native WPF fold and twist snapshots rendered and visually inspected (physics-fold.png and physics-twist.png).
- Setup extraction verified all five payload files against build output by SHA-256.
- Installed locally with Setup --install; exit code 0, registry DisplayVersion 0.2.0, and all five installed payload hashes match build output.
- Existing MLM theme remained selected. The installed overlay is visible with extended style 0x8080080: toolwindow/noactivate, without appwindow. No Start or desktop shortcut is created; normal mode has no tray implementation.
- Full icon drag-under-rug interaction, Windows 10, mixed-DPI and uninstall coverage remain as listed above. No claim of 60 FPS or arbitrary-tangle nonintersection is made.
## 0.2.1 verification — 2026-10-18

- The 0.2.0 candidate was rolled back after user-reported lag; it was not published as a GitHub release.
- Nine physics/input suites passed after the final material change: Cloth, Physics, Bending, Grip, Stress, DesktopPhysics, Roll, Contact and DesktopMode. Contact geometry fixtures also passed with the desktop neighbor radius of one.
- Corner locality and decorative-corner drag tests failed before the fix and passed afterward. The desktop far-corner displacement is 11.958 px for the one-second corner pull.
- Six NativePlacement assertions pass: missing desktop, null predecessor, temporarily raised desktop, existing correct placement, stable anchor, and show/restore z-order protection. The final app and installer were rebuilt after these changes, rerunning NativePlacement and DesktopMode.
- Native WPF twist snapshot generated from the final build and visually inspected (physics-twist.png). physics-fold.png is an older dense candidate snapshot.
- Window placement avoids HWND_TOP fallback, preserves z-order during WPF show/restore, and falls back below windows while Explorer's desktop host is temporarily above applications. The original transient Explorer flash has not yet been manually reproduced against this build.
- Solver performance and remaining desktop contact creep are documented in physics-baseline.md. Passing dense-grid settling tests do not certify desktop-grid settling.
Installation checks for 0.2.1: Setup --install exited successfully; registry DisplayVersion is 0.2.1. All five extracted and installed payload SHA-256 hashes match the final build. The existing MLM theme remains selected. One visible overlay has extended style 0x8080080, with no appwindow style. A read-only native z-order probe found overlay rank 21 immediately above desktop rank 22. No MLM Desktop shortcut exists in the user/common Start or Desktop locations checked. These checks do not substitute for reproducing the transient Explorer-folder flash.