# Performance and video implementation

1. Record baseline puzzle generation time/allocations in the existing Play Mode validator, plus first-frame timing.
2. Optimize pixel generation and picker allocations; release unnecessary CPU texture storage, cap FPS, and remove unused PC 3D render passes.
3. Add cancellable, silent near-completion video preparation and reuse it on completion; shorten the fade.
4. Remove proven dead sample code and legacy assets, preserving asset references and current media.
5. Run regression/performance validation, review renders, and update README with measured results.

The writing-plans skill is unavailable; this uses the repository's existing plan format. Scope is restricted to the user's requested resource use, cleanup and video delay.

## Verification — 2026-10-21

All 181 Play Mode checks passed. Measured generation decreased from 725.9/845.4 ms to 496.2/609.2 ms for 4x4/8x8. The local fixture's cold first frame decreased from 257.1 to 142.7 ms; its prepared first frame took 6.5 ms. The 0.20-second fade, silent near-completion preparation, prepared target reuse, menu pause, cancellation, CPU pixel release, alpha hits, completion artwork release, and completed-picture resizing passed. Picture/frame render is pixel-identical to the baseline. Removed 16 reference-checked legacy images (13,534,807 bytes), matching metadata, and dead procedural generation. Current user media preserved.

GC thread allocation counter returned zero in this Unity configuration and was not used as a reliable metric. See docs/performance.md for measurement scope and limitations. Final cleanup resets alpha hit testing before clearing the image sprite to avoid irrelevant Unity warnings.

Final verification after disabling the remaining camera-only 3D postprocessing and removing alpha-clear warnings: all 181 checks passed again. Latest generation times: 491.1/611.0 ms; cold/warm fixture first-frame times: 99.5/11.4 ms. Picture/frame capture remains pixel-identical. Final-run log contains no alpha-clear warnings, compile errors, missing/null references, or validation failures. No pending editor requests remain.

## User-requested reveal and exit refinement — 2026-10-21

Restore a 0.25-second finished-photo hold and the former 0.45-second fade, preserving silent preparation and the other resource improvements. Delay actual playback/audio until the hold ends; pause/cancel it with the existing playback lifecycle. Remove the eager StopPlayback call from the Exit button so its render target survives until scene teardown. Extend real Play Mode checks to cover the hold, visible photo, and a video-playing Exit request before teardown.

Verified: all 184 Play Mode checks passed in an isolated local copy using the same runtime/controller/scene/validator source hashes. Warm first-frame timing is 259.4 ms including the intentional 250 ms hold; the 450 ms reveal is preserved. The actual Exit button was invoked with a displayed video, and ExitingPlayMode confirmed that its image and RenderTexture remained intact before destruction. Reports and before-exit capture copied to Logs/GalleryPreview. Batch validation explicitly drives the player loop and clears error pause so coroutines progress without a visible Game view.

Temporary validation editor exited. Cleanup of Temp/RevealExitValidation-20261021 was rejected by automatic approval policy ("blocked by policy"), including a retry with the verified literal path. The ignored temporary copy remains; no editor request is pending in the main project.
