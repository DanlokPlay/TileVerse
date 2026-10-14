# Stable puzzle interaction and preview

User-requested revision: improve loose-piece readability, remove deformed assembled fragments outside the frame, and add a preview setting. The user chose a configurable placement lock rather than mandatory locking.

## Interaction

Keep group dragging and assembly outside the frame. Hover uses a subtle outline and never changes geometry. Increase the preferred loose-piece scale from 0.72 to 0.9 within existing side-area bounds. Picking up a single piece changes it to full assembly size while keeping the grabbed point beneath the pointer. Connected fragments always remain at full assembly scale, including in side areas; their relative positions never change when the pointer enters or leaves.

A group records whether it is placed at its correct coordinates. A correctly placed group anchors nearby merging pieces without being displaced by them. A setting named «Закреплять в рамке», enabled by default, prevents dragging placed groups. Turning the setting off enables dragging them again. Starting such a drag clears the placement state; dropping near the correct location restores it. Resizing preserves placement, group connections, and the selected locking behavior. Keep dropped groups within the visible play area without shrinking them.

## Settings

Add adjacent «Предосмотр» and «Закреплять в рамке» checkboxes to the compact TMP settings panel. Both apply immediately without restarting or discarding assembly progress, and remain selected when the menu reopens or puzzle settings are applied.

Preview defaults to enabled and controls only the faint target photo behind the pieces. Disabling it leaves the selected background visible inside the frame. The menu thumbnail remains available. Completion always displays the seamless completed photo before video, including when preview is disabled, and preserves the photo on video failure.

## Implementation and verification

The group owns placement state; the controller owns drag/merge/snap rules and live preferences. PieceView owns hover highlighting. SettingsView owns checkbox bindings and visual state; the scene installer authors and connects permanent controls.

Verify pointer-stable pickup, unchanged hover scale, connected fragments remaining intact in side areas, locked/unlocked drag behavior, merging into placed groups without displacement, keeping groups visible, preview changes without rebuilding pieces, completion with preview disabled, and menu fitting. Run the existing Play Mode checks and render both menu and gameplay.
