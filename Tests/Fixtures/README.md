# Gallery playback fixture

`gallery-video.mp4` is an eight-second H.264 Baseline procedural color pattern with BT.709 metadata and a sine-wave audio track, generated with FFmpeg. It contains no third-party media. It is kept outside Assets and is not shipped in the game.

Run **Tools → TileVerse → Validate gallery in Play Mode** to exercise the saved scene. Results and screenshots are written to `Logs/GalleryPreview`. In batch mode, use `-executeMethod PuzzleGalleryValidation.Run` without `-quit`; the validator exits Unity with a success/failure code after Play Mode ends.
