# Coherent Master Frame Implementation Plan

1. Generate one transparent classic gold master frame with quiet, stretch-friendly straight sides and ornate corners.
2. Inspect its alpha and composition, then store the master and eight exact crops under `Assets/Art/Frames/Coherent/`.
3. Change the scene installer to use the coherent crops for its eight outer `Image` components, disable the old generated inner fragments, and use non-tiled edge images.
4. Rebuild `SampleScene`, verify the saved transform layout and Sprite references, then compile and commit.
