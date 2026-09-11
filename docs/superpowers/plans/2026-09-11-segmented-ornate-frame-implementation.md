# Segmented Ornate Frame — Implementation Plan

1. Generate transparent gold-carved corner, horizontal edge, vertical edge, and thin inner-trim sprites.
2. Store the selected sprites under `Assets/Art/Frames/` and set their Unity imports for tiled UI use.
3. Extend `PuzzleFrameView` and the scene installer with editable corner and edge Image references plus adaptive layout rules.
4. Rebuild `SampleScene`, verify the scene hierarchy and compile the gameplay/editor assemblies.
