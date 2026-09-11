# Coherent Master Frame Design

## Goal

Replace the independently generated frame fragments with parts cut from one master image, so every corner, edge, colour and carved detail belongs to the same physical-looking classical frame.

## Asset strategy

- Generate one square, transparent-background master image of a warm antique-gold gallery frame with a clear central opening.
- Keep dense acanthus carving in the four corners and use calmer, linear carved relief on the straight sections; this preserves the look when sides stretch to fit wide, square or portrait photos.
- Derive eight transparent sprites from exact regions of that master: four corners plus top, bottom, left and right edges.
- Do not tile the derived straight sections. Stretch each one between its adjacent corners, so no repeated pattern creates a visible break.
- Reuse the existing `Gold Trim` UI image for the thin inner line. It remains a separate, editable scene layer, but is not a separately generated asset.

## Scene layout

`Frame Template / Ornate Frame Parts` retains individual editable UI Images:

```text
Top Left Corner       Top Edge       Top Right Corner
Left Edge                              Right Edge
Bottom Left Corner   Bottom Edge    Bottom Right Corner
```

Each element receives a crop from the same master image. The right and bottom elements use mirrored copies of their source crop, positioned inside their anchors. The `PuzzleOrnateFrameView` keeps one common width for every outer piece and positions the eight elements whenever photo proportions change.

## Acceptance criteria

- Corners meet straight edges without gaps, overlap, or a change in frame width.
- The frame holds together for landscape, square and portrait photos.
- All eight outer parts and the built-in `Gold Trim` remain editable in `SampleScene`.
- The assembled scene has no centered 100×100 placeholder parts in edit mode.
