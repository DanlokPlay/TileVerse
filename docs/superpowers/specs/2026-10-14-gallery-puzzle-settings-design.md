# Gallery puzzle settings

Approved in chat on 2026-10-14. This extends the existing adaptive living puzzle and its ornate frame.

## Experience

- Start directly in the framed puzzle. Fit the complete photo inside the available board area without stretching or cropping. Reflow the frame and pieces when the window changes size, preserving assembly progress.
- Esc opens a polished gallery settings panel: warm gold accents, a photo preview, image/video selection, two piece-form choices, row/column sliders (2–8), and white/black background choices. Fit the complete panel inside the window.
- Classic pieces use complementary random tabs. Tiles have straight edges; their proportions follow the photo and chosen grid, so rectangular photos can produce rectangular tiles.
- Image, video, grid, and piece-form changes are pending until Apply. Confirm a restart if a puzzle is in progress. Closing settings discards pending changes. Background changes apply immediately without resetting the puzzle.
- Completion shows a seamless still picture, then fades in the selected video within the picture area. Preserve video aspect ratio with letterboxing if needed. Pause playback while settings are open. Missing or invalid video leaves the completed photo visible with a short message.

## Implementation

Extend the current controller and scene installer. Keep permanent UI objects serialized in SampleScene and create only puzzle pieces at runtime. Reuse the existing ornate frame art. Piece mode belongs to active/pending puzzle settings, independently of media. The theme component provides exact white/black surfaces with legible panel palettes. The board layout owns fit calculations; the controller remaps existing piece groups during resizing. CompletionVideo owns video preparation, first-frame fade, pause, cancellation, and render target lifetime.

## Validation

Compile both runtime and editor assemblies through Unity. Exercise classic/tile generation, portrait/square/landscape layouts, resizing with assembled groups, pending-setting cancellation and restart, exact background colors, completion fallback, and video aspect/pause behavior. Render and inspect the game and settings panel. Report any playback scenario that cannot be exercised with available local media.
