using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleFrameView : MonoBehaviour
{
    [SerializeField] private RectTransform frameRoot;
    [SerializeField] private RectTransform outerFrame;
    [SerializeField] private RectTransform innerShadow;
    [SerializeField] private RectTransform goldTrim;
    [SerializeField] private RectTransform mat;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Image outerFrameImage;
    [SerializeField] private Image innerShadowImage;
    [SerializeField] private Image goldTrimImage;
    [SerializeField] private Image matImage;

    public RectTransform ContentRoot => contentRoot;

    public void Configure(
        RectTransform root,
        RectTransform outer,
        RectTransform shadow,
        RectTransform trim,
        RectTransform matRect,
        RectTransform content,
        Image outerImage,
        Image shadowImage,
        Image trimImage,
        Image matLayerImage)
    {
        frameRoot = root;
        outerFrame = outer;
        innerShadow = shadow;
        goldTrim = trim;
        mat = matRect;
        contentRoot = content;
        outerFrameImage = outerImage;
        innerShadowImage = shadowImage;
        goldTrimImage = trimImage;
        matImage = matLayerImage;
    }

    public Vector2 Apply(PuzzleFrameStyle style, Vector2 contentSize)
    {
        if (frameRoot == null)
        {
            return contentSize;
        }

        var outer = Mathf.Max(1f, style.outerWidth);
        var shadow = Mathf.Max(0f, style.shadowWidth);
        var trim = Mathf.Max(0f, style.trimWidth);
        var matInset = Mathf.Max(0f, style.matWidth);
        var totalInset = outer + shadow + trim + matInset;

        frameRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentSize.x + totalInset * 2f);
        frameRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentSize.y + totalInset * 2f);

        Stretch(outerFrame, 0f);
        Stretch(innerShadow, outer);
        Stretch(goldTrim, outer + shadow);
        Stretch(mat, outer + shadow + trim);

        if (contentRoot != null)
        {
            contentRoot.anchorMin = new Vector2(0.5f, 0.5f);
            contentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            contentRoot.pivot = new Vector2(0.5f, 0.5f);
            contentRoot.anchoredPosition = Vector2.zero;
            contentRoot.sizeDelta = contentSize;
        }

        if (outerFrameImage != null) outerFrameImage.color = style.outerColor;
        if (innerShadowImage != null) innerShadowImage.color = style.shadowColor;
        if (goldTrimImage != null) goldTrimImage.color = style.trimColor;
        if (matImage != null) matImage.color = style.matColor;

        return frameRoot.rect.size;
    }

    public float GetTotalInset(PuzzleFrameStyle style) => style.TotalInset;

    private static void Stretch(RectTransform rect, float inset)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }
}
