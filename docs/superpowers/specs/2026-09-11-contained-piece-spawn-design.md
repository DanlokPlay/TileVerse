# Contained Puzzle-Piece Spawn — Design Specification

## Goal

Ensure every puzzle piece is fully visible on the first frame of a new puzzle. The change applies only to random initial placement; players may still drag pieces anywhere during assembly.

## Cause

The left and right spawn areas use asymmetric pivots: the left area is anchored from its left edge and the right area from its right edge. The current random-position calculation treats both as centre-pivoted rectangles, so its horizontal range can extend beyond the actual local rectangle.

## Design

`PuzzleGameController.BuildSpawnPosition` will derive its usable range from the spawn area's actual `RectTransform.rect` values instead of half of its width and height.

- The random centre point is constrained between `rect.xMin + pieceHalfWidth + padding` and `rect.xMax - pieceHalfWidth - padding`.
- The same calculation uses `rect.yMin` and `rect.yMax` vertically.
- The full scaled piece bounds, including puzzle tabs, are therefore contained inside the assigned spawn area and visible viewport.
- If a workspace is too narrow or short for a piece, its centre is placed at the area's geometric centre as a safe fallback.
- The existing random distribution, left/right alternation, overlap relaxation, board layout, and drag behaviour remain unchanged.

## Verification

1. Start 2x2 through 8x8 puzzles with landscape, square, and portrait photos.
2. Confirm each initial piece is entirely visible on both side workspaces at the configured reference resolution.
3. Repeat in narrower and taller Game view resolutions.
4. Confirm pieces can still be dragged beyond the side areas after spawning.
