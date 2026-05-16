using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PuzzlePieceView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private PuzzleGameController controller;

    public RectTransform RectTransform { get; private set; }
    public Sprite PieceSprite { get; private set; }
    public int Row { get; private set; }
    public int Column { get; private set; }
    public Vector2 CorrectAnchoredPosition { get; private set; }
    public PuzzlePieceGroup Group { get; private set; }

    private void Awake()
    {
        RectTransform = (RectTransform)transform;
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
