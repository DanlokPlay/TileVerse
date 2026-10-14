# Clean frame and compact settings

Approved revision after user review on 2026-10-14. The user confirmed a compact window with all settings, a preset background palette, and a custom color picker.

## Frame

Use the complete transparent OrnateGoldMaster artwork as one tiled UI Image, preserving the original corner proportions. Its 320-pixel corner regions include approximately 200-pixel-wide rails and a soft transparent inner fringe; they must not be forced into equally sized squares and strips. Repeat the straight rails at the same uniform scale as the corners, cropping the final repeat rather than stretching the ornament on wide or tall photos. Keep the frame immediately around the picture and retain the original transparent outer silhouette.

User-requested refinement: calibrate the photo opening at 192 source pixels, underneath the opaque gold edge, to eliminate the transparent seam visible on a white background. Keep the full corner proportions for extreme photo formats.

Remove the colored outer backing, inner shadow panel, separate gold trim, mat, and picture backdrop. Keep only the frame artwork, photo/puzzle guide, pieces, and completion video presentation. No additional brown rectangles, decorative outlines, or padding bands. Frame dimensions continue to follow photo aspect and available window size.

## Menu

Use a compact centered settings card with all primary controls visible.

User-requested refinement: increase the menu, its text, and its controls to 140% of the original size (12% larger than the preceding revision). Apply the same readable scale to its dialogs. Reduce this scale only when needed to keep the whole panel inside the window with a margin.

- Plain title and close control; remove marketing title, subtitle, branding, numbered section titles, technical dimensions, repeated hints, and built-in example buttons from the visible menu.
- Small photo preview and clearly labeled photo/video selection with readable file names.
- Two adjacent piece-form choices.
- Row/column numeric controls with minus/plus buttons and direct integer entry, limited to 2–8. Show the total piece count once.
- Background palette with restrained neutral and muted colors. Black and white remain available. A custom-color action opens a saturation/value area, hue control, and HEX input. Background applies live without resetting the puzzle.
- One Apply action for pending photo/video/form/grid changes; closing leaves the current puzzle intact. Confirm restart only when necessary. Esc first closes the color picker, then the settings menu.

All user-visible text, including button labels, input text, validation messages, and completion messages, uses TextMeshPro. Include a reusable font asset with Cyrillic glyphs. Preserve all fixed menu objects in SampleScene.

## Default media

Use the existing `Assets/Resources/ispu_1.png` and `ispu_1.mp4` as the startup photo/video pair, replacing the Mountains and Glade sample entries. Preserve the source photo aspect without power-of-two import rescaling. Save both references in the scene and use the same pair when no entries have been configured. Selecting different files through settings remains available.

Playback fills the same opening as the completed photo with uniform scaling and a centered crop. Do not shrink the full encoded video into the picture: ispu_1 contains side bars in its 1920×1080 frame, while its actual picture matches the photo aspect. Filling the opening crops these side bars and avoids adding top/bottom padding. Other video aspect ratios also use a centered crop without distortion.

## Responsibilities and checks

FrameView fits the picture and one frame Image. A focused settings view owns UI controls, grid entry validation, selection state, and media labels. A color-picker component owns HSV/HEX conversion and pointer interaction. The game controller retains active/pending gameplay settings and restart decisions. ThemeView applies the background color while keeping menu colors legible.

Verify corner proportions, transparent silhouette, removal of colored frame layers, photo alignment for landscape/square/portrait images, TMP-only labels and Cyrillic coverage, numeric grid boundaries, preset/custom colors, Esc behavior, pending cancellation/restart, resize preservation, and completion video. Render and inspect the scene and menu after rebuilding. Preserve a scene copy before rebuilding an open editor scene.
