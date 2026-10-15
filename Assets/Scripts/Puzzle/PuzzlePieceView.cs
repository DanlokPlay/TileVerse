using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PuzzlePieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private PuzzleGameController controller;
    private Outline outline;

    public RectTransform RectTransform { get; private set; }
    public Sprite PieceSprite { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public Vector2 CorrectAnchoredPosition { get; private set; }
    public PuzzlePieceGroup Group { get; private set; }

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
        outline = GetComponent<Outline>();
    }

    public void Initialize(PuzzleGameController owner, Sprite sprite, int row, int column, Vector2 correctAnchoredPosition)
    {
        controller = owner;
        PieceSprite = sprite;
        Row = row;
        Column = column;
        CorrectAnchoredPosition = correctAnchoredPosition;
    }

    public void AssignGroup(PuzzlePieceGroup group)
    {
        Group = group;
    }

    public void SetCorrectPosition(Vector2 position)
    {
        CorrectAnchoredPosition = position;
    }

    public void SetHighlighted(bool highlighted)
    {
        if (outline == null) return;
        outline.effectColor = highlighted ? new Color(0.83f, 0.69f, 0.46f, 0.65f) : new Color(0f, 0f, 0f, 0.3f);
        outline.effectDistance = highlighted ? new Vector2(1.6f, -1.6f) : new Vector2(1.2f, -1.2f);
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
