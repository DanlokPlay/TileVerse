# Adaptive Living Puzzle — Implementation Plan

> Design: `docs/superpowers/specs/2026-09-04-adaptive-living-puzzle-design.md`

## Scope and principles

- Preserve the existing puzzle-piece masking, dragging, merging, and snapping behaviour.
- Keep every persistent visual/UI object in `Assets/Scenes/SampleScene.unity`; create only puzzle pieces at runtime.
- Do not distort or crop a selected photo.
- Treat the central board and both side spawn zones as one responsive layout system.
- Keep the change confined to the puzzle scripts, `SampleScene`, and any necessary video render asset.

## 1. Establish editable scene hierarchy

**Files:**

- Modify `Assets/Scenes/SampleScene.unity`

**Work:**

1. Remove/disable the separate landing canvas from the launch flow and repurpose the existing setup canvas into `Settings Canvas`.
2. Restructure the game canvas into `Left Pieces Area`, `Board Stage`, `Right Pieces Area`, and a full-screen `Pieces Layer`.
3. Under `Board Stage`, create `Frame Template` with `Outer Frame`, `Inner Shadow`, `Gold Trim`, `Mat`, `Puzzle Guide`, and `Completion Video Overlay` children.
4. Create a settings overlay with photo/video selection, grid controls, theme toggle, Apply/Cancel controls, and a separate inactive reset-confirmation dialog.
5. Add a scene `VideoPlayer`, `AudioSource` if required by the chosen audio output mode, `RenderTexture`, and `RawImage` overlay. Ensure the overlay is constrained to the frame's content rectangle.
6. Rename scene objects and assign serialized fields as they are created, so the scene remains legible and editable in the Inspector.

**Checks:**

- Opening the scene shows all persistent layout elements in the Hierarchy.
- The default active view at Play start is the game canvas only.
- No static UI is instantiated from code.

## 2. Add artwork, frame-style, and theme configuration data

**Files:**

- Add `Assets/Scripts/Puzzle/PuzzleMediaEntry.cs`
- Add `Assets/Scripts/Puzzle/PuzzleFrameStyle.cs`
- Add `Assets/Scripts/Puzzle/PuzzleTheme.cs`

**Work:**

1. Define a serializable media entry containing display name, image texture, optional `VideoClip`, custom-video URL state, frame-style reference/data, and loop setting.
2. Define a serializable frame style containing layer colours, outer width, inner shadow width, trim width, mat inset, and relevant alpha settings.
3. Define a theme palette and editable scene bindings for the light and dark variants.
4. Give the existing mountains and glade images default media-entry records; video remains safely optional until clips are assigned in the Inspector.

**Checks:**

- An entry can be created and adjusted from the controller Inspector without code changes.
- A different style changes only the selected frame layers.
- A missing video is a valid configuration and has a completion fallback.

## 3. Implement the adaptive board and dual-spawn layout

**Files:**

- Add `Assets/Scripts/Puzzle/PuzzleBoardLayout.cs`
- Add `Assets/Scripts/Puzzle/PuzzleFrameView.cs`
- Modify `Assets/Scripts/Puzzle/PuzzleGameController.cs`

**Work:**

1. Calculate the maximum board content rectangle from screen size, safe vertical padding, frame insets, and left/right minimum workspace widths (18% each).
2. Fit the selected texture into that rectangle by aspect ratio, then grow the frame around the content rectangle using the selected style measurements.
3. Resize and position the four frame layers and the puzzle guide without stretching the source image.
4. Derive left and right safe spawn rectangles from the board's rendered bounds, assign a random side to each initial piece, and select positions that do not overlap the frame or leave the screen.
5. Update correct piece positions, snap thresholds, and guide placement to use the computed board-content rectangle rather than fixed top/side padding.
6. Recompute the layout after settings changes and when screen dimensions change.

**Checks:**

- 16:9 landscape, 1:1 square, and 2:3 portrait images are never cropped or stretched.
- Wide images retain both side zones; portrait and square images use near-full height.
- Every starting piece is within a side zone, with both zones receiving pieces when more than one piece exists.
- Existing grouping and final snapping still align to the image.

## 4. Replace launch and menu flow with the Esc settings overlay

**Files:**

- Modify `Assets/Scripts/Puzzle/PuzzleGameController.cs`
- Add `Assets/Scripts/Puzzle/PuzzleSettingsController.cs`
- Add `Assets/Scripts/Puzzle/NativeVideoPicker.cs`
- Modify `Assets/Scenes/SampleScene.unity`

**Work:**

1. Start the first valid configured media entry directly from `Awake`/`Start`, replacing `ShowMenuScreen` as the default flow.
2. Detect `Esc` through the enabled input system and toggle the settings overlay; close it with Esc without applying pending game-affecting values.
3. Maintain active and pending configuration snapshots. Theme changes apply live; image, video, and grid changes remain pending until Apply.
4. On Apply, compare configurations. Show the confirmation dialog only when image, video, or grid differs; confirmation clears the active puzzle and starts the new configuration, while cancellation changes nothing.
5. Reuse the existing image picker for custom photos; add a Windows/editor video picker for common video files and record the local file URL for the active session.
6. Keep the visible game screen free of buttons, labels, title, and setup controls whenever the settings overlay is closed.

**Checks:**

- Play mode opens a playable board directly.
- Esc opens and closes settings without losing an untouched game.
- A theme switch changes colours immediately and preserves piece positions.
- Changing photo, clip, or grid opens confirmation; Cancel leaves the puzzle untouched and Confirm resets it.
- Cancelled file pickers do not alter pending or active configuration.

## 5. Add completion-video playback and graceful fallback

**Files:**

- Add `Assets/Scripts/Puzzle/PuzzleCompletionVideo.cs`
- Modify `Assets/Scripts/Puzzle/PuzzleGameController.cs`
- Modify `Assets/Scenes/SampleScene.unity`

**Work:**

1. Configure the scene VideoPlayer to render to the completion overlay's RenderTexture and use the selected `VideoClip` or custom local URL.
2. On puzzle completion, lock interactions, prepare the player, and fade the overlay only after successful preparation.
3. Set audio output, loop state, first-frame behaviour, completion hold, and error callbacks.
4. On player error or absent video, leave the fully assembled still image in place and show a compact message through the settings/status layer without re-enabling puzzle movement.
5. Stop and clear video output before starting a different puzzle or destroying the controller.

**Checks:**

- A configured clip fades in inside the frame and plays with audio once, retaining its final frame.
- Looping works when enabled on the media entry.
- A missing/bad clip cannot block the puzzle or hide the completed still.
- Resetting during or after playback clears video state cleanly.

## 6. Verify and clean up

**Files:**

- Modified puzzle scripts and `Assets/Scenes/SampleScene.unity`

**Work:**

1. Compile the project and address Unity Console errors/warnings introduced by the feature.
2. Run manual Play Mode scenarios from the design specification at more than one Game view aspect ratio.
3. Confirm all serialized scene references are assigned and no gameplay UI is unintentionally visible on game start.
4. Inspect the final diff to ensure only puzzle/UI assets and scripts changed.

**Acceptance checklist:**

- The game starts immediately in clean assembly mode.
- The board is centrally framed, aspect-correct, and responsive.
- Pieces spawn randomly to the left and right of the picture.
- Esc exposes all requested settings and protects in-progress work with confirmation.
- The selected artwork pair plays video inside the completed frame.
- Frame and UI remain easy to adjust through the scene and Inspector.
