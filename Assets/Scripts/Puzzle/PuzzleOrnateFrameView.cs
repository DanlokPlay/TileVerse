using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleOrnateFrameView : MonoBehaviour
{
    public const float PictureInsetPixels = 192f;
    public const float CornerPixels = 320f;

    [SerializeField, Min(1f)] private float railWidth = 82f;
    [SerializeField] private Image frameImage;

    // Start the photo under the opaque rail, before its transparent inner fringe.
    public float RequiredOuterWidth => frameImage != null && frameImage.sprite != null ? railWidth : 0f;
    public Image FrameImage => frameImage;

    public void Configure(Image image)
    {
        frameImage = image;
    }

    public void Apply(float inset)
    {
        if (frameImage == null || frameImage.sprite == null) return;
        var rect = frameImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        frameImage.type = Image.Type.Tiled;
        frameImage.fillCenter = false;
        frameImage.color = Color.white;
        // Tile the straight rails at the same scale as the corners.
        frameImage.pixelsPerUnitMultiplier = PictureInsetPixels / Mathf.Max(0.0001f, inset);
    }
}
