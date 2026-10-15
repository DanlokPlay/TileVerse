using UnityEngine;

public sealed class PuzzleBoardLayout : MonoBehaviour
{
    [SerializeField] [Range(0.1f, 0.35f)] private float minimumSideWorkspaceRatio = 0.18f;
    [SerializeField] [Min(0f)] private float horizontalGap = 18f;
    [SerializeField] [Min(0f)] private float verticalPadding = 28f;
    [SerializeField] private RectTransform canvasRoot;
    [SerializeField] private RectTransform boardStage;
    [SerializeField] private RectTransform leftPiecesArea;
    [SerializeField] private RectTransform rightPiecesArea;
    [SerializeField] private PuzzleFrameView frameView;

    public RectTransform LeftPiecesArea => leftPiecesArea;
    public RectTransform RightPiecesArea => rightPiecesArea;

    public void Configure(
        RectTransform canvas,
        RectTransform stage,
        RectTransform leftArea,
        RectTransform rightArea,
        PuzzleFrameView frame)
    {
        canvasRoot = canvas;
        boardStage = stage;
        leftPiecesArea = leftArea;
        rightPiecesArea = rightArea;
        frameView = frame;
    }

    public Vector2 Apply(Texture texture, PuzzleFrameStyle frameStyle)
    {
        if (canvasRoot == null || boardStage == null || frameView == null || texture == null)
        {
            return Vector2.zero;
        }

        Canvas.ForceUpdateCanvases();
        var canvasSize = canvasRoot.rect.size;
        if (canvasSize.x < 1f || canvasSize.y < 1f)
        {
            return Vector2.zero;
        }

        var sideWidth = Mathf.Min(Mathf.Max(canvasSize.x * minimumSideWorkspaceRatio, 120f), canvasSize.x * 0.25f);
        var totalInset = frameView.GetTotalInset(frameStyle);
        var availableWidth = Mathf.Max(1f, canvasSize.x - sideWidth * 2f - horizontalGap * 2f);
        var availableHeight = Mathf.Max(1f, canvasSize.y - verticalPadding * 2f);
        var frameScale = Mathf.Min(1f, availableWidth / (totalInset * 2f + 180f), availableHeight / (totalInset * 2f + 180f));
        var maxContentWidth = Mathf.Max(1f, availableWidth - totalInset * 2f * frameScale);
        var maxContentHeight = Mathf.Max(1f, availableHeight - totalInset * 2f * frameScale);
        var imageAspect = texture.width / (float)Mathf.Max(1, texture.height);
        var contentSize = FitAspect(imageAspect, maxContentWidth, maxContentHeight);

        boardStage.anchorMin = new Vector2(0.5f, 0.5f);
        boardStage.anchorMax = new Vector2(0.5f, 0.5f);
        boardStage.pivot = new Vector2(0.5f, 0.5f);
        boardStage.anchoredPosition = Vector2.zero;
        boardStage.sizeDelta = frameView.Apply(frameStyle, contentSize, frameScale);

        ApplySpawnArea(leftPiecesArea, true, sideWidth, canvasSize.y - verticalPadding * 2f);
        ApplySpawnArea(rightPiecesArea, false, sideWidth, canvasSize.y - verticalPadding * 2f);
        return contentSize;
    }

    private static Vector2 FitAspect(float aspect, float maxWidth, float maxHeight)
    {
        var width = maxWidth;
        var height = width / Mathf.Max(0.01f, aspect);
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    private static void ApplySpawnArea(RectTransform rect, bool left, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(left ? 0f : 1f, 0.5f);
        rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }
}
