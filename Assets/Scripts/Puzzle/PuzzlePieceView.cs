using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PuzzlePieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private PuzzleGameController controller;
    private Outline outline;
    private Image image;

    public RectTransform RectTransform { get; private set; }
    public Sprite PieceSprite { get; private set; }
    public Vector2 SourcePixelSize { get; private set; }
    public Rect ArtworkBoundsNormalized { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public Vector2 CorrectAnchoredPosition { get; private set; }
    public PuzzlePieceGroup Group { get; private set; }

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
        outline = GetComponent<Outline>();
        image = GetComponent<Image>();
    }

    public void Initialize(PuzzleGameController owner, Sprite sprite, int row, int column, Vector2 correctAnchoredPosition)
    {
        controller = owner;
        PieceSprite = sprite;
        SourcePixelSize = sprite.rect.size;
        var min = Vector2.one;
        var max = Vector2.zero;
        foreach (var vertex in sprite.vertices)
        {
            var pixel = vertex * sprite.pixelsPerUnit + sprite.pivot;
            var normalized = new Vector2(pixel.x / SourcePixelSize.x, pixel.y / SourcePixelSize.y);
            min = Vector2.Min(min, normalized);
            max = Vector2.Max(max, normalized);
        }
        ArtworkBoundsNormalized = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        Row = row;
        Column = column;
        CorrectAnchoredPosition = correctAnchoredPosition;
    }

    public void AssignGroup(PuzzlePieceGroup group)
    {
        Group = group;
    }

    public void ReleaseArtwork()
    {
        if (image != null)
        {
            image.enabled = false;
            image.alphaHitTestMinimumThreshold = 0f;
            image.sprite = null;
        }
        if (PieceSprite == null) return;
        var texture = PieceSprite.texture;
        Destroy(PieceSprite);
        PieceSprite = null;
        if (texture != null) Destroy(texture);
    }

    public void SetCorrectPosition(Vector2 position)
    {
        CorrectAnchoredPosition = position;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline == null) return;
        outline.effectColor = highlighted ? new Color(0.35f, 0.78f, 1f, 0.9f) : new Color(0f, 0f, 0f, 0.3f);
        outline.effectDistance = highlighted ? new Vector2(2f, -2f) : new Vector2(1.2f, -1.2f);
        outline.useGraphicAlpha = true;
    }

    public Rect GetArtworkRect()
    {
        var rect = RectTransform.rect;
        var scale = (Vector2)RectTransform.localScale;
        var position = RectTransform.anchoredPosition;
        var min = position + Vector2.Scale(rect.min + Vector2.Scale(rect.size, ArtworkBoundsNormalized.min), scale);
        var max = position + Vector2.Scale(rect.min + Vector2.Scale(rect.size, ArtworkBoundsNormalized.max), scale);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        controller.BeginDrag(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        controller.Drag(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        controller.EndDrag(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        controller.BeginHover(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.EndHover(this);
    }
}
