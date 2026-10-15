# Clean frame and compact settings — implementation

1. Replace independent frame fragments and colored layers with a sliced transparent master Image. Preserve native 320:200 corner/rail geometry and align the photo opening to the rails.
2. Add a focused settings view using TMP labels, integer grid steppers, media preview, form controls, palette swatches, and one Apply action. Add an HSV/HEX picker with live background changes and Esc handling.
3. Migrate controller and completion messages to TMP, connect the settings view, and keep active/pending gameplay decisions in the controller.
4. Import licensed Liberation Sans/TMP resources and create a font asset with Cyrillic coverage. Rebuild the scene through the open editor while keeping a recoverable scene copy.
5. Update integration validation for the new controls, frame geometry, palette/custom color, TMP-only text, resize, and video. Render and inspect the final game and menu.

The writing-plans skill is unavailable; this plan follows the existing repository format. The approved specification was reviewed for scope, consistency, and missing requirements.

## Completed verification — 2026-10-14

The saved scene and both assemblies were updated in Unity 6000.3.13f1. The Play Mode validator passed 103 assertions, including transparent frame geometry, original corner proportions, extreme aspect ratios, TMP-only labels and Cyrillic coverage, grid entry/steppers, palette and HSV/HEX interaction, pending settings, resize preservation, and completion video.

Visual review covered the game, compact menu, color picker, portrait layout, and video fallback. Local missing/empty videos are rejected before preparation; failure to receive a decoded frame has a bounded timeout and leaves the completed photo visible. Scene copies were preserved under Temp before rebuilding.

## Requested visual refinements

Calibrate the picture inset to the opaque rail at 192 source pixels, preserving original corner proportions and the minimum space for both corners. Increase the preferred settings/dialog scale to 1.25 while retaining window fitting. Rebuild the saved scene and review the photo against white, the enlarged menu, and narrow-window behavior through the existing Play Mode validator.

Completed: saved scene rebuilt; both updated assemblies compiled and all 103 Play Mode checks passed. Reviewed `photo-frame-white.png`, `settings-white.png`, and `color-picker.png`: the photo meets the gold edge without a background stripe, and the larger controls remain inside their panels. Narrow-window fitting and extreme-aspect frame checks also passed.

## Ornament scale and default-media revision

Use tiled frame rails with the same scale as the native corners; inspect the repeats on landscape and portrait pictures. Increase the preferred menu/dialog scale from 1.25 to 1.4, with existing window fitting. Replace legacy sample scene entries with the ispu_1 photo and VideoClip pair, preserve the photo import aspect, and use the same defaults at runtime when no entries are configured. Rebuild the scene, run the existing integration validation, and verify actual ispu_1 playback after completion.

Completed: the saved scene uses tiled rails, a 1.4 menu scale, and the ispu_1 pair. All 108 Play Mode checks passed, including the original photo aspect and decoding the bundled clip after completion. Visual review of landscape and portrait frames confirmed that the ornament retains its proportions; the menu remains within the window. The user's version and icon changes in ProjectSettings were preserved.

## Video scale correction

The ispu_1 video contains a photo-shaped picture with baked side bars in a 1920×1080 frame. FitInside added another pair of bars above/below it. Change playback to FitOutside so the picture fills the photo opening without distortion. Verify the four rendered edges of the default video, plus existing playback, pause/resume, fallback, and responsive layout checks; inspect the final completion render.

Completed: all 112 Play Mode checks passed. The decoded default video reaches all four photo edges without black padding, and the completion render was visually reviewed. Validation uses looping copies of short clips and waits for resumed playback to avoid timing failures; background execution is enabled only during validation and restored afterward.
