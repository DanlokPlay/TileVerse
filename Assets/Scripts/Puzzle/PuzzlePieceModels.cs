using UnityEngine;

public enum PuzzlePieceMode
{
    Classic,
    Tiles
}

public enum PuzzlePieceSizeMode
{
    Dynamic,
    Original
}

public sealed class PuzzleSourceImage
{
    public string DisplayName { get; }
    public Texture2D Texture { get; set; }

    public PuzzleSourceImage(string displayName, Texture2D texture)
    {
        DisplayName = displayName;
        Texture = texture;
    }
}

public struct PuzzlePieceShape
{
    public int Top;
    public int Right;
    public int Bottom;
    public int Left;
}

public readonly struct PuzzleSliceBounds
{
    public int XMin { get; }
    public int XMax { get; }
    public int YMin { get; }
    public int YMax { get; }
    public int Width => XMax - XMin;
    public int Height => YMax - YMin;

    public PuzzleSliceBounds(int xMin, int xMax, int yMin, int yMax)
    {
        XMin = xMin;
        XMax = xMax;
        YMin = yMin;
        YMax = yMax;
    }
}

public readonly struct PuzzleLayoutMetrics
{
    public float UiScale { get; }
    public Vector2 PuzzleSize { get; }
    public Vector2 MaxPieceUiSize { get; }
    public float PieceGapUi { get; }
    public int TabMarginSource { get; }
    public float BoardSnapThreshold { get; }
    public float MergeThreshold { get; }
    public Vector2 BoardGuideLocalCenter { get; }

    public PuzzleLayoutMetrics(
        float uiScale,
        Vector2 puzzleSize,
        Vector2 maxPieceUiSize,
        float pieceGapUi,
        int tabMarginSource,
        float boardSnapThreshold,
        float mergeThreshold,
        Vector2 boardGuideLocalCenter)
    {
        UiScale = uiScale;
        PuzzleSize = puzzleSize;
        MaxPieceUiSize = maxPieceUiSize;
        PieceGapUi = pieceGapUi;
        TabMarginSource = tabMarginSource;
        BoardSnapThreshold = boardSnapThreshold;
        MergeThreshold = mergeThreshold;
        BoardGuideLocalCenter = boardGuideLocalCenter;
    }
}
