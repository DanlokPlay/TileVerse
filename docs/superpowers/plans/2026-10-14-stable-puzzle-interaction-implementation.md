# Stable puzzle interaction — implementation

1. Add group placement state and enforce it through snapping, merging, dragging, and resizing. Keep connected groups at full scale and constrain released groups to the visible play area.
2. Replace hover scaling with highlighting, increase preferred loose-piece scale, and preserve the grabbed point when changing pickup scale.
3. Add live preview/placement-lock preferences to the controller and two serialized TMP checkbox controls to the settings panel. Preserve completion and error presentation independently of preview.
4. Rebuild the editable scene with a recoverable copy, extend Play Mode validation for the interaction rules and toggles, and inspect rendered results.

The writing-plans skill is unavailable in this environment; this follows the existing repository plan format. The design was checked for scope, consistency, and complete behavior before implementation.

## Completed verification — 2026-10-15

Both current assemblies compiled and the editable scene was rebuilt with a recoverable copy. All 132 Play Mode checks passed, including pointer-stable pickup, hover without scaling, full-scale connected fragments in side areas, visible release bounds, configurable locking, merging without moving a placed anchor, resize preservation, live preview persistence, and completion/video with preview disabled.

Reviewed the rendered settings, preview-off gameplay, and connected fragment outside the frame. Both checkboxes fit in the enlarged panel, all labels use TMP, and disabling preview leaves the selected background visible within the frame. The preferred loose-piece scale is 0.9, bounded by the side workspace.

## Hover and free-drag refinement — 2026-10-15

Reproduced the user's snap-back with a regression: an unlocked fragment dropped 48×36 UI units from its place returned to the board because the old snap radius scaled up with the piece size. Limit snapping to small screen-space distances, attach dragging to the grabbed point, and constrain movement before release. Restore hover enlargement for loose pieces and use uniform group enlargement around a common pivot. Add a secondary Exit button that quits builds and stops Play Mode in the editor. Verify free drops, hover/pickup stability, seams, boundary dragging, and actual Exit wiring in the existing Play Mode validator.

Completed: both updated assemblies compiled, the saved scene was rebuilt, and all 140 Play Mode checks passed. The original free-drop regression now passes; hovered group pickup preserves the grabbed point, repeated hover does not accumulate drift, and connected seams remain intact. The validator finishes by invoking the actual Exit button and confirming the return to Edit Mode. Reviewed the rendered menu with its new footer action.

## Tall fragment movement correction — 2026-10-15

The user clarified that a connected fragment from a portrait image moved horizontally but scarcely vertically. Full-group screen clamping caused this: a nearly screen-height group had little vertical clearance. Let connected groups follow the grabbed point without full-visibility constraints. On release and resize, leave partially visible groups alone and recover only groups that leave an entire screen axis. Preserve loose-piece fitting and all hover/snap/lock behavior. Exercise a screen-height fragment using top, middle, and bottom grips in both directions, release without a jump, and recovery after a completely offscreen drop.

Completed: current runtime/editor assemblies compiled and all 149 Play Mode checks passed. A screen-height portrait fragment follows top, middle, and bottom grips vertically in both directions, including when its edges leave the screen; partially visible releases do not move it. Fully offscreen release recovery and full group scale also passed. Reviewed the tall-fragment render. No scene/UI changes were required for this correction.

## Single-piece movement correction — 2026-10-15

The user requested the same freedom for individual loose pieces. Remove their drag-time full-visibility clamp and apply the existing offscreen-release recovery to pieces and groups alike. Preserve pickup, hover, snapping, and resting scales. Verify exact pointer following at each screen edge, partially visible release without repositioning, free dragging beyond the screen, and recovery only after a fully offscreen release.

Completed: runtime/editor assemblies compiled and all 165 Play Mode checks passed. Sixteen new checks cover each of the four edges: pointer following, stable partially visible release, unconstrained movement completely beyond the screen, and accessible recovery on release. Existing group dragging, hover/pickup, placement locking, snapping, preview, video, and Exit checks also passed. No scene/UI changes were required.
