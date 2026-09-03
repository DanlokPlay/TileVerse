# Adaptive Living Puzzle — Design Specification

## Goal

Turn the existing TileVerse puzzle game into a clean gallery-like experience. The game starts directly in puzzle assembly mode: an adaptive framed picture sits in the centre of the screen, and loose pieces are randomly placed in workspace zones on its left and right. When the player completes the puzzle, the completed artwork animates by playing the video assigned to that image inside the same frame.

All permanent UI and presentation objects must be authored in `Assets/Scenes/SampleScene.unity` so they can be adjusted in Unity's Hierarchy and Inspector. Runtime code may create only individual puzzle-piece objects.

## Confirmed experience

### Gameplay screen

- The old landing screen and persistent gameplay buttons, labels, and hints are removed from the assembly screen.
- At launch, the game opens a configured image/video pair immediately. It uses the most recently applied pair during this run, otherwise the first configured built-in pair.
- The central picture is surrounded by a universal, multilayer, classic gold frame. It is constructed from editable UI elements rather than decorative image assets.
- Loose pieces initially spawn randomly, split between the left and right workspace zones. They can be dragged across the whole gameplay area and assembled in the central picture.
- The picture preserves its native aspect ratio. It is neither cropped nor stretched.
- The board takes as much vertical space as possible while the left and right workspaces retain at least 18% of the screen width each. For very wide pictures, this width constraint lowers the board below its maximum height. Square and portrait pictures therefore appear almost full-height; wide pictures remain large but leave usable side space.

### Frame template

`FrameTemplate` is a scene object that contains these independently editable layers:

1. `OuterFrame` — the broad outer gold/brown band.
2. `InnerShadow` — the dimensional shadow just inside the outer band.
3. `GoldTrim` — the narrow bright decorative edge.
4. `Mat` — the neutral inset around the picture.

The layout component resizes the frame container according to the selected texture's aspect ratio. Each visual layer stretches only as a UI panel and has editable colour, thickness, inset, and opacity. A frame style is selected per configured artwork pair, allowing individual colours and widths without duplicating gameplay logic.

### Settings and reset behaviour

- Pressing `Esc` opens `Settings Canvas` as an overlay over the current puzzle.
- The overlay contains a light/dark theme switch, an artwork pair selector, photo selection, video selection, and row/column controls for the puzzle grid.
- A custom image and the chosen local video form a pair for the current application session.
- A theme change applies immediately and does not reset the puzzle.
- Changing the image, video, or grid makes the current puzzle stale. `Apply` then opens `Reset Confirmation Dialog`; confirmation discards the current assembly and begins a new one. Cancelling preserves both the board and the in-progress puzzle.
- Pressing `Esc` again closes the settings overlay without applying pending changes.

### Completion video

- Completion locks puzzle movement and removes gameplay-only status text.
- `Completion Video Overlay` fades in over the picture content but remains inside the visible frame.
- A Unity `VideoPlayer` renders the assigned video to the overlay. Video sound is enabled.
- By default, the video plays once and leaves its final frame visible; looping is an Inspector-configurable option.
- If a video cannot be selected, prepared, or played, the correctly assembled still image remains visible and the player gets a compact error message rather than a broken screen.

## Scene architecture

```text
Puzzle Game Root
├─ Gameplay Canvas
│  ├─ Left Pieces Area
│  ├─ Board Stage
│  │  ├─ Frame Template
│  │  │  ├─ Outer Frame
│  │  │  ├─ Inner Shadow
│  │  │  ├─ Gold Trim
│  │  │  └─ Mat
│  │  ├─ Puzzle Guide
│  │  └─ Completion Video Overlay
│  ├─ Right Pieces Area
│  └─ Pieces Layer
├─ Settings Canvas
├─ Reset Confirmation Dialog
└─ Event System
```

Static RectTransforms, Images, RawImages, buttons, text, theme bindings, and VideoPlayer references reside in `SampleScene`. The game controller holds Inspector references to those objects. It does not construct persistent UI hierarchy at runtime.

## Components and responsibilities

### PuzzleGameController

Retains existing puzzle lifecycle, piece generation, grouping, snapping, merging, and completion evaluation. It owns screen state transitions and coordinates the components below.

### PuzzleBoardLayout

Calculates the central board size from the selected texture, screen size, configured safe vertical padding, and the two minimum side-zone widths. It updates the frame, puzzle guide, video overlay, and spawn-area RectTransforms. It provides left/right safe rectangles for random piece placement and prevents pieces from spawning over the framed board.

### PuzzleFrameView and FrameStyle

`PuzzleFrameView` applies the selected `FrameStyle` to the scene's frame-layer Images. `FrameStyle` contains editable UI colours, border widths, insets, and alpha values. Built-in artwork entries may each reference a style. No texture asset is required for the initial frame version.

### PuzzleMediaEntry and media selection

Each configured entry has a display name, a `Texture2D` image, an optional `VideoClip`, a loop option, and an optional frame style. Custom selections use a loaded texture and a local video path. The selected entry is the single source for puzzle image, board guide, and completion-video source.

### PuzzleSettingsController and theme bindings

Maintains a pending settings snapshot while the Esc panel is open. It compares that snapshot with the active puzzle configuration before applying. Theme bindings in the scene map light and dark palette values to their target UI Graphics, ensuring static scene objects remain directly editable.

### PuzzleCompletionVideo

Configures the scene VideoPlayer and RenderTexture/RawImage output. It handles preparation, playback, fade, sound output, end-of-video behaviour, and failure fallback.

## State and data flow

1. Startup selects the default media entry and builds the puzzle immediately.
2. Layout computes central frame dimensions, resizes frame layers and content area, and produces the two spawn regions.
3. The generator creates pieces; each piece is assigned a randomized valid point in either side region.
4. Dragging, merging, snapping, and completion remain governed by the existing puzzle logic.
5. `Esc` copies active settings into a pending settings model and opens the overlay.
6. `Apply` either changes theme immediately or, for game-affecting settings, requests reset confirmation.
7. A confirmed reset clears runtime pieces, applies media/grid/frame selections, recomputes layout, and starts a new puzzle.
8. Completion triggers video preparation and an in-frame fade. Failure keeps the assembled still visible.

## Failure handling

- Missing scene references: report an explicit setup error in the editor/development build and avoid starting a malformed game.
- No selected photo: retain the prior valid configuration and keep `Apply` unavailable.
- Invalid or unavailable video: allow the puzzle to play, then keep the still image at completion and show a concise message.
- Invalid grid: preserve the existing 2–8 limit and prevent invalid values from applying.
- A cancelled file dialog or reset confirmation makes no state changes.

## Verification

Manual Unity Play Mode verification will cover:

1. Landscape, square, and portrait textures at multiple Game view resolutions: no crop/stretch, a visible adaptive frame, and usable left/right spawn areas.
2. Random piece placement in both side zones with no initial spawn over the board.
3. Piece dragging, group merging, snapping, and final completion after the layout changes.
4. Direct game start, Esc toggle, Esc close, light/dark theme changes, and pending-setting cancellation.
5. Confirmation required for image, video, or grid changes; cancellation preserves the active puzzle.
6. Completion-video prepare, playback with audio, last-frame hold, and optional looping.
7. Video-missing/error fallback leaves the assembled picture visible.

## Scope boundaries

- The initial frame is intentionally a universal multilayer UI frame. Ornate frame sprites can be introduced later by replacing the layer images without changing the gameplay layout contract.
- Custom media pairing is session-only in this feature. Persisting arbitrary local file paths across launches is outside the initial scope.
- The feature changes `SampleScene` and puzzle-related scripts only; it does not add another scene or a backend.
