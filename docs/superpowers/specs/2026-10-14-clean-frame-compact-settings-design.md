# Clean frame and compact settings

Approved revision after user review on 2026-10-14. The user confirmed a compact window with all settings, a preset background palette, and a custom color picker.

## Frame

Use the complete transparent OrnateGoldMaster artwork as one sliced UI Image, preserving the original corner proportions. Its 320-pixel corner regions include approximately 200-pixel-wide rails and a soft transparent inner fringe; they must not be forced into equally sized squares and strips. Keep the frame immediately around the picture and retain the original transparent outer silhouette.

User-requested refinement: calibrate the photo opening at 192 source pixels, underneath the opaque gold edge, to eliminate the transparent seam visible on a white background. Keep the full corner proportions for extreme photo formats.

Remove the colored outer backing, inner shadow panel, separate gold trim, mat, and picture backdrop. Keep only the frame artwork, photo/puzzle guide, pieces, and completion video presentation. No additional brown rectangles, decorative outlines, or padding bands. Frame dimensions continue to follow photo aspect and available window size.

## Menu

Use a compact centered settings card with all primary controls visible.

User-requested refinement: increase the menu, its text, and its controls by 25%. Apply the same readable scale to its dialogs. Reduce this scale only when needed to keep the whole panel inside the window with a margin.

- Plain title and close control; remove marketing title, subtitle, branding, numbered section titles, technical dimensions, repeated hints, and built-in example buttons from the visible menu.
- Small photo preview and clearly labeled photo/video selection with readable file names.
- Two adjacent piece-form choices.
- Row/column numeric controls with minus/plus buttons and direct integer entry, limited to 2–8. Show the total piece count once.
- Background palette with restrained neutral and muted colors. Black and white remain available. A custom-color action opens a saturation/value area, hue control, and HEX input. Background applies live without resetting the puzzle.
- One Apply action for pending photo/video/form/grid changes; closing leaves the current puzzle intact. Confirm restart only when necessary. Esc first closes the color picker, then the settings menu.

All user-visible text, including button labels, input text, validation messages, and completion messages, uses TextMeshPro. Include a reusable font asset with Cyrillic glyphs. Preserve all fixed menu objects in SampleScene.

## Responsibilities and checks

FrameView fits the picture and one frame Image. A focused settings view owns UI controls, grid entry validation, selection state, and media labels. A color-picker component owns HSV/HEX conversion and pointer interaction. The game controller retains active/pending gameplay settings and restart decisions. ThemeView applies the background color while keeping menu colors legible.

Verify corner proportions, transparent silhouette, removal of colored frame layers, photo alignment for landscape/square/portrait images, TMP-only labels and Cyrillic coverage, numeric grid boundaries, preset/custom colors, Esc behavior, pending cancellation/restart, resize preservation, and completion video. Render and inspect the scene and menu after rebuilding. Preserve a scene copy before rebuilding an open editor scene.
