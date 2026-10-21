# Runtime resource use and completion latency

User request: reduce resource use and project clutter, and shorten the pause before completion video. Preserve the current appearance, puzzle interaction, editable scene, and media choices.

## Changes

Cap normal rendering at 60 FPS instead of unlimited rendering. Remove unused 3D render passes from the UI-only PC renderer without reducing UI resolution. Generate piece textures through direct RGBA32 buffers, retaining readable piece pixels for alpha hit testing but releasing the completed source texture's CPU copy. Reuse the color-picker workspace. Remove dead procedural sample generation and only legacy artwork proven unreferenced; keep current/user media.

Release generated piece artwork on completion, keeping lightweight placement metadata so the completed picture can still resize correctly. Release decoder/render-target resources when cancelling playback or starting a different puzzle.

Prepare the selected video once a connected fragment contains at least 75% of the puzzle, without playing audio or showing video. Completion reuses matching prepared media instead of restarting preparation. Keep cold playback, first-frame protection, menu pausing, cancellation on a new puzzle, missing-file fallback, and bounded preparation.

User refinement on 2026-10-21: restore the more deliberate reveal. Show the completed photo for 0.25 seconds after preparation, then start video/audio with the original 0.45-second fade. Menu pause also suspends this short hold. Cancelling or replacing playback cancels the hold. Exit requests must leave the displayed video and its texture intact until Unity tears down the scene/window; release resources in OnDestroy rather than before requesting exit.

## Verification

Record real Play Mode generation time and managed allocations before/after for 4x4 and 8x8. Retain all existing interaction/playback checks; add checks for transparent hit testing, released source CPU memory, picker allocations, silent preparation and reuse, cancellation and late errors. Measure video first-frame latency through frameReady events. Review rendered output and document measured results and their limits.
