using UnityEngine;

public sealed class PuzzleFrameView : MonoBehaviour
{
    [SerializeField] private RectTransform frameRoot;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private PuzzleOrnateFrameView ornateFrame;

    public RectTransform ContentRoot => contentRoot;
    public RectTransform FrameRoot => frameRoot;

    public void Configure(RectTransform root, RectTransform content, PuzzleOrnateFrameView ornament)
    {
        frameRoot = root;
        contentRoot = content;
        ornateFrame = ornament;
    }

    public Vector2 Apply(PuzzleFrameStyle style, Vector2 contentSize, float frameScale = 1f)
    {
        if (frameRoot == null) return contentSize;
        // Keep room for both original corners even when the photo is very narrow.
        var minimumOpening = 2f * PuzzleOrnateFrameView.CornerPixels / PuzzleOrnateFrameView.PictureInsetPixels - 2f;
        var inset = Mathf.Min(GetTotalInset(style) * frameScale, Mathf.Min(contentSize.x, contentSize.y) / minimumOpening);
        frameRoot.sizeDelta = contentSize + Vector2.one * inset * 2f;
        if (contentRoot != null)
        {
            contentRoot.anchorMin = contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = contentSize;
        }
        ornateFrame?.Apply(inset);
        return frameRoot.rect.size;
    }

    public float GetTotalInset(PuzzleFrameStyle style) => ornateFrame != null ? ornateFrame.RequiredOuterWidth : 0f;
}
