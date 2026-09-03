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

        var sideWidth = Mathf.Max(canvasSize.x * minimumSideWorkspaceRatio, 120f);
        var totalInset = frameView.GetTotalInset(frameStyle);
        var maxContentWidth = Mathf.Max(180f, canvasSize.x - sideWidth * 2f - horizontalGap * 2f - totalInset * 2f);
        var maxContentHeight = Mathf.Max(180f, canvasSize.y - verticalPadding * 2f - totalInset * 2f);
        var imageAspect = texture.width / (float)Mathf.Max(1, texture.height);
        var contentSize = FitAspect(imageAspect, maxContentWidth, maxContentHeight);

        boardStage.anchorMin = new Vector2(0.5f, 0.5f);
        boardStage.anchorMax = new Vector2(0.5f, 0.5f);
        boardStage.pivot = new Vector2(0.5f, 0.5f);
        boardStage.anchoredPosition = Vector2.zero;
        boardStage.sizeDelta = frameView.Apply(frameStyle, contentSize);

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
