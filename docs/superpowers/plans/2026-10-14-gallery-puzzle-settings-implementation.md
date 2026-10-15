# Gallery puzzle settings — implementation

Design: ../specs/2026-10-14-gallery-puzzle-settings-design.md

1. Add a classic/tile generation mode and pending/active form selection. Use zero texture margins for tiles. Include form changes in restart confirmation.
2. Add photo preview and styled form/background choices in the scene installer. Fit the settings panel to the canvas. Use pure white/black backgrounds and accessible panel colors.
3. Reflow frame and existing groups on canvas-size changes. Update correct positions, piece dimensions, and snap metrics without resetting progress. Fit loose pieces into their side areas.
4. Size the video render target for the picture area, preserve its aspect ratio, fade after the first decoded frame, pause in settings, and cancel preparing playback when restarting. Reveal a seamless still on completion.
5. Rebuild SampleScene through Unity, run integration validation, render previews, inspect output, and repair any failures.

The writing-plans skill is not installed in the available skill directories; this explicit plan follows the repository's existing plan format.

## Verification completed — 2026-10-14

Unity 6000.3.13f1 compiled the runtime/editor assemblies and rebuilt SampleScene successfully. The Play Mode validator passed 86 assertions covering generation, restart/cancel, exact background colors, tile margins, portrait reflow and merged groups, completion, MP4 decoding, playback pause/resume, aspect preservation, and invalid-video fallback. Game, settings, portrait, video, and fallback renders were inspected under `Logs/GalleryPreview`.

Visual review also corrected nested frame insets, joined the ornament rails beneath the corners, added a black video matte, and placed completion messages inside the picture area.
