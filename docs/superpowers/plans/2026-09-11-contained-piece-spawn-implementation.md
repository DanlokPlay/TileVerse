# Contained Piece Spawn — Implementation Plan

1. Replace centre-pivot assumptions in `PuzzleGameController.BuildSpawnPosition` with the actual local bounds from `RectTransform.rect`.
2. Keep the piece half-size and existing padding inside those bounds; retain the area-centre fallback for undersized workspaces.
3. Compile the gameplay assembly and run a focused static check confirming the corrected range is present.
