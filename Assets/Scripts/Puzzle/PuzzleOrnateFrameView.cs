using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleOrnateFrameView : MonoBehaviour
{
    [Header("Editable dimensions")]
    [SerializeField] [Min(1f)] private float ornamentWidth = 136f;
    [SerializeField] [Min(1f)] private float cornerSizeMultiplier = 1f;

    [Header("Scene ornament parts")]
    [SerializeField] private RectTransform ornamentRoot;
    [SerializeField] private Image topLeftCorner;
    [SerializeField] private Image topRightCorner;
    [SerializeField] private Image bottomLeftCorner;
    [SerializeField] private Image bottomRightCorner;
    [SerializeField] private Image topEdge;
    [SerializeField] private Image bottomEdge;
    [SerializeField] private Image leftEdge;
    [SerializeField] private Image rightEdge;

    public float RequiredOuterWidth => HasVisibleSprites ? ornamentWidth : 0f;

    public void Configure(
        RectTransform root,
        Image topLeft, Image topRight, Image bottomLeft, Image bottomRight,
        Image top, Image bottom, Image left, Image right)
    {
        ornamentRoot = root;
        topLeftCorner = topLeft;
        topRightCorner = topRight;
        bottomLeftCorner = bottomLeft;
        bottomRightCorner = bottomRight;
        topEdge = top;
        bottomEdge = bottom;
        leftEdge = left;
        rightEdge = right;
    }

    public void UseCoherentMasterPreset()
    {
        ornamentWidth = 136f;
        cornerSizeMultiplier = 1f;
    }

    public void Apply(float outerWidth)
    {
        if (ornamentRoot == null)
        {
            return;
        }

        Stretch(ornamentRoot, 0f);
        if (!HasVisibleSprites)
        {
            return;
        }

        var edgeWidth = Mathf.Min(Mathf.Max(1f, ornamentWidth), outerWidth);
        var cornerSize = edgeWidth * Mathf.Max(1f, cornerSizeMultiplier);
        SetCorner(topLeftCorner, new Vector2(0f, 1f), new Vector2(0f, 1f), cornerSize);
        SetCorner(topRightCorner, new Vector2(1f, 1f), new Vector2(1f, 1f), cornerSize);
        SetCorner(bottomLeftCorner, new Vector2(0f, 0f), new Vector2(0f, 0f), cornerSize);
        SetCorner(bottomRightCorner, new Vector2(1f, 0f), new Vector2(1f, 0f), cornerSize);

        SetTopOrBottomEdge(topEdge, true, cornerSize, edgeWidth);
        SetTopOrBottomEdge(bottomEdge, false, cornerSize, edgeWidth);
        SetLeftOrRightEdge(leftEdge, true, cornerSize, edgeWidth);
        SetLeftOrRightEdge(rightEdge, false, cornerSize, edgeWidth);
    }

    private bool HasVisibleSprites =>
        HasSprite(topLeftCorner) || HasSprite(topRightCorner) || HasSprite(bottomLeftCorner) || HasSprite(bottomRightCorner) ||
        HasSprite(topEdge) || HasSprite(bottomEdge) || HasSprite(leftEdge) || HasSprite(rightEdge);

    private static bool HasSprite(Image image) => image != null && image.sprite != null;

    private static void SetCorner(Image image, Vector2 anchor, Vector2 pivot, float size)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.one * size;
        rect.localScale = Vector3.one;
    }

    private static void SetTopOrBottomEdge(Image image, bool top, float cornerSize, float edgeWidth)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.offsetMin = new Vector2(cornerSize * 0.96f, top ? -edgeWidth : 0f);
        rect.offsetMax = new Vector2(-cornerSize * 0.96f, top ? 0f : edgeWidth);
        rect.localScale = Vector3.one;
    }

    private static void SetLeftOrRightEdge(Image image, bool left, float cornerSize, float edgeWidth)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rect.offsetMin = new Vector2(left ? 0f : -edgeWidth, cornerSize * 0.96f);
        rect.offsetMax = new Vector2(left ? edgeWidth : 0f, -cornerSize * 0.96f);
        rect.localScale = Vector3.one;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
