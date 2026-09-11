using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleOrnateFrameView : MonoBehaviour
{
    [Header("Editable dimensions")]
    [SerializeField] [Min(1f)] private float ornamentWidth = 112f;
    [SerializeField] [Min(1f)] private float cornerSizeMultiplier = 1.22f;
    [SerializeField] [Min(1f)] private float innerTrimWidth = 20f;

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
    [SerializeField] private Image topInnerTrim;
    [SerializeField] private Image bottomInnerTrim;
    [SerializeField] private Image leftInnerTrim;
    [SerializeField] private Image rightInnerTrim;

    public float RequiredOuterWidth => HasVisibleSprites ? ornamentWidth : 0f;

    public void Configure(
        RectTransform root,
        Image topLeft, Image topRight, Image bottomLeft, Image bottomRight,
        Image top, Image bottom, Image left, Image right,
        Image trimTop, Image trimBottom, Image trimLeft, Image trimRight)
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
        topInnerTrim = trimTop;
        bottomInnerTrim = trimBottom;
        leftInnerTrim = trimLeft;
        rightInnerTrim = trimRight;
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
        var trimWidth = Mathf.Min(Mathf.Max(1f, innerTrimWidth), edgeWidth);

        SetCorner(topLeftCorner, new Vector2(0f, 1f), new Vector2(0f, 1f), cornerSize, Vector2.zero, Vector3.one);
        SetCorner(topRightCorner, new Vector2(1f, 1f), new Vector2(1f, 1f), cornerSize, new Vector2(-cornerSize, 0f), new Vector3(-1f, 1f, 1f));
        SetCorner(bottomLeftCorner, new Vector2(0f, 0f), new Vector2(0f, 0f), cornerSize, new Vector2(0f, cornerSize), new Vector3(1f, -1f, 1f));
        SetCorner(bottomRightCorner, new Vector2(1f, 0f), new Vector2(1f, 0f), cornerSize, new Vector2(-cornerSize, cornerSize), new Vector3(-1f, -1f, 1f));

        SetTopOrBottomEdge(topEdge, true, cornerSize, edgeWidth, Vector3.one);
        SetTopOrBottomEdge(bottomEdge, false, cornerSize, edgeWidth, new Vector3(1f, -1f, 1f));
        SetLeftOrRightEdge(leftEdge, true, cornerSize, edgeWidth, Vector3.one);
        SetLeftOrRightEdge(rightEdge, false, cornerSize, edgeWidth, new Vector3(-1f, 1f, 1f));

        SetTopOrBottomTrim(topInnerTrim, true, cornerSize, edgeWidth, trimWidth, Vector3.one);
        SetTopOrBottomTrim(bottomInnerTrim, false, cornerSize, edgeWidth, trimWidth, new Vector3(1f, -1f, 1f));
        SetLeftOrRightTrim(leftInnerTrim, true, cornerSize, edgeWidth, trimWidth, Vector3.one);
        SetLeftOrRightTrim(rightInnerTrim, false, cornerSize, edgeWidth, trimWidth, new Vector3(-1f, 1f, 1f));
    }

    private bool HasVisibleSprites =>
        HasSprite(topLeftCorner) || HasSprite(topRightCorner) || HasSprite(bottomLeftCorner) || HasSprite(bottomRightCorner) ||
        HasSprite(topEdge) || HasSprite(bottomEdge) || HasSprite(leftEdge) || HasSprite(rightEdge);

    private static bool HasSprite(Image image) => image != null && image.sprite != null;

    private static void SetCorner(Image image, Vector2 anchor, Vector2 pivot, float size, Vector2 position, Vector3 scale)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = Vector2.one * size;
        rect.localScale = scale;
    }

    private static void SetTopOrBottomEdge(Image image, bool top, float cornerSize, float edgeWidth, Vector3 scale)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.offsetMin = new Vector2(cornerSize * 0.58f, top ? -edgeWidth : 0f);
        rect.offsetMax = new Vector2(-cornerSize * 0.58f, top ? 0f : edgeWidth);
        rect.anchoredPosition = new Vector2(0f, top ? 0f : edgeWidth);
        rect.localScale = scale;
    }

    private static void SetLeftOrRightEdge(Image image, bool left, float cornerSize, float edgeWidth, Vector3 scale)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rect.offsetMin = new Vector2(left ? 0f : -edgeWidth, cornerSize * 0.58f);
        rect.offsetMax = new Vector2(left ? edgeWidth : 0f, -cornerSize * 0.58f);
        rect.anchoredPosition = new Vector2(left ? 0f : -edgeWidth, 0f);
        rect.localScale = scale;
    }

    private static void SetTopOrBottomTrim(Image image, bool top, float cornerSize, float edgeWidth, float trimWidth, Vector3 scale)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
        rect.offsetMin = new Vector2(cornerSize * 0.62f, top ? -edgeWidth : edgeWidth - trimWidth);
        rect.offsetMax = new Vector2(-cornerSize * 0.62f, top ? -edgeWidth + trimWidth : edgeWidth);
        rect.anchoredPosition = new Vector2(0f, top ? 0f : edgeWidth);
        rect.localScale = scale;
    }

    private static void SetLeftOrRightTrim(Image image, bool left, float cornerSize, float edgeWidth, float trimWidth, Vector3 scale)
    {
        if (image == null) return;
        var rect = image.rectTransform;
        rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
        rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rect.offsetMin = new Vector2(left ? edgeWidth - trimWidth : -edgeWidth, cornerSize * 0.62f);
        rect.offsetMax = new Vector2(left ? edgeWidth : -edgeWidth + trimWidth, -cornerSize * 0.62f);
        rect.anchoredPosition = new Vector2(left ? 0f : -edgeWidth, 0f);
        rect.localScale = scale;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
