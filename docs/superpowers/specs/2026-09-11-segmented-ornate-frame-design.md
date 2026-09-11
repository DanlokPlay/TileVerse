# Segmented Ornate Gold Frame — Design Specification

## Goal

Add one universal, classic carved-gold picture frame that works with built-in and custom photos of any aspect ratio. The frame must look decorative rather than flat, preserve its corners without distortion, and remain editable in `SampleScene`.

## Visual direction

- Classic warm gold gallery frame based on the selected balanced concept and the first user reference.
- Visible raised floral/scrollwork in the corners, a carved profile along each side, dark brown depth in recesses, and a thin bright inner gold edge.
- No text, logos, picture, or opaque centre: the frame assets have transparent backgrounds outside their ornament.

## Asset set

Generate five transparent PNG sprites:

1. `OrnateGoldCorner` — a single square corner with the complete carved flourish.
2. `OrnateGoldHorizontalEdge` — a seamless horizontal decorative strip.
3. `OrnateGoldVerticalEdge` — a seamless vertical decorative strip.
4. `OrnateGoldInnerTrimHorizontal` — a thin seamless bright inner-gold strip.
5. `OrnateGoldInnerTrimVertical` — a thin seamless bright inner-gold strip.

The corner sprite is mirrored by scene objects for the other three corners. Edge sprites use tiled UI images, so their ornament repeats instead of stretching. The inner trim sprites make the join with the mat look intentional at any board size.

## Scene hierarchy

`Gameplay Canvas / Board Stage / Frame Template` gains an editable `Ornate Frame Parts` group:

```text
Ornate Frame Parts
├─ Top Left Corner
├─ Top Right Corner
├─ Bottom Left Corner
├─ Bottom Right Corner
├─ Top Edge
├─ Bottom Edge
├─ Left Edge
├─ Right Edge
├─ Top Inner Trim
├─ Bottom Inner Trim
├─ Left Inner Trim
└─ Right Inner Trim
```

Each is a standard `Image` with a visible Sprite and editable RectTransform. Corners have fixed square sizes. Edges are laid out between the corners and use `Image.Type = Tiled`. Existing `Inner Shadow` and `Mat` remain behind the parts; the legacy flat `Gold Trim` remains available as a subtle fallback layer.

## Runtime layout

`PuzzleFrameView` receives serialized references to the segmented images and updates their anchors and offsets every time it sizes the board.

- A new editable `ornamentWidth` value defines the physical width of the ornate frame.
- Corner sizes are based on this width and remain square.
- Each side begins and ends after the appropriate corner, so no side strip overlaps a corner.
- The image content rectangle stays unchanged: puzzle sizing, guide position, video overlay clipping, and side workspaces retain their existing behaviour.
- The segmented art is universal for all media entries; individual entries keep their existing editable colours and layer widths.

## Failure handling

- If a decorative sprite is missing, the existing flat UI frame still draws and the puzzle remains playable.
- If a tiled edge cannot repeat exactly at a particular size, Unity crops the final repeat inside the corner boundary rather than scaling the pattern.

## Verification

1. Confirm all twelve ornament objects are visible in `SampleScene` and editable from the Hierarchy.
2. Test landscape, square, and portrait images: corners retain their shape, edges do not stretch, and no gaps appear at joins.
3. Verify the picture, puzzle guide, and completion video remain inside the inner trim.
4. Confirm a missing sprite leaves the existing basic UI frame functional.
