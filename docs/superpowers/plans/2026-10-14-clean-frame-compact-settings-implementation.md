# Clean frame and compact settings — implementation

1. Replace independent frame fragments and colored layers with a sliced transparent master Image. Preserve native 320:200 corner/rail geometry and align the photo opening to the rails.
2. Add a focused settings view using TMP labels, integer grid steppers, media preview, form controls, palette swatches, and one Apply action. Add an HSV/HEX picker with live background changes and Esc handling.
3. Migrate controller and completion messages to TMP, connect the settings view, and keep active/pending gameplay decisions in the controller.
4. Import licensed Liberation Sans/TMP resources and create a font asset with Cyrillic coverage. Rebuild the scene through the open editor while keeping a recoverable scene copy.
5. Update integration validation for the new controls, frame geometry, palette/custom color, TMP-only text, resize, and video. Render and inspect the final game and menu.

The writing-plans skill is unavailable; this plan follows the existing repository format. The approved specification was reviewed for scope, consistency, and missing requirements.
