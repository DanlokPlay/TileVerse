# Stable puzzle interaction and preview

User-requested revision: improve loose-piece readability, remove deformed assembled fragments outside the frame, and add a preview setting. The user chose a configurable placement lock rather than mandatory locking.

## Interaction

Keep group dragging and assembly outside the frame. Loose pieces rest slightly reduced (preferred scale 0.9 within the side-area bounds) and enlarge to full assembly size on hover, with a subtle outline. Connected fragments rest at full assembly scale everywhere and enlarge uniformly to 1.04 on hover, scaling both artwork and spacing around one common pivot. Pointer exit restores their exact geometry. Picking up any hovered piece restores assembly scale while preserving the grabbed point. Dragging follows that point absolutely; screen limits apply during movement. Board and neighbor snapping are limited to small screen-space distances so unlocked fragments do not jump back after an intentional free drop. Suppress other hover enlargement during dragging.

A group records whether it is placed at its correct coordinates. A correctly placed group anchors nearby merging pieces without being displaced by them. A setting named «Закреплять в рамке», enabled by default, prevents dragging placed groups. Turning the setting off enables dragging them again. Starting such a drag clears the placement state; dropping near the correct location restores it. Resizing preserves placement, group connections, and the selected locking behavior. Keep dropped groups within the visible play area without shrinking them.

## Settings

Add adjacent «Предосмотр» and «Закреплять в рамке» checkboxes to the compact TMP settings panel. Both apply immediately without restarting or discarding assembly progress, and remain selected when the menu reopens or puzzle settings are applied.

Preview defaults to enabled and controls only the faint target photo behind the pieces. Disabling it leaves the selected background visible inside the frame. The menu thumbnail remains available. Completion always displays the seamless completed photo before video, including when preview is disabled, and preserves the photo on video failure.

Add a secondary «Выход» button alongside «Применить». It quits the built application; in the Unity editor it ends Play Mode. Closing the settings with the upper close control keeps its existing behavior.

## Implementation and verification

The group owns placement state; the controller owns drag/merge/snap rules and live preferences. PieceView owns hover highlighting. SettingsView owns checkbox bindings and visual state; the scene installer authors and connects permanent controls.

Verify pointer-stable pickup from an enlarged piece/group, hover enlargement and exact restoration, connected fragments remaining intact in side areas, locked/unlocked drag behavior without free-drop snap-back, merging into placed groups without displacement, keeping groups visible during dragging, preview changes without rebuilding pieces, completion with preview disabled, the Exit button, and menu fitting. Run the existing Play Mode checks and render both menu and gameplay.
