using UnityEngine;
using UnityEngine.EventSystems;

public sealed class PuzzleColorSurface : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    public PuzzleColorPicker Picker;
    public bool IsHue;

    public void OnPointerDown(PointerEventData eventData) => ReadPointer(eventData);
    public void OnDrag(PointerEventData eventData) => ReadPointer(eventData);

    private void ReadPointer(PointerEventData data)
    {
        var rect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out var point)) return;
        var bounds = rect.rect;
        var x = Mathf.InverseLerp(bounds.xMin, bounds.xMax, point.x);
        var y = Mathf.InverseLerp(bounds.yMin, bounds.yMax, point.y);
        if (IsHue) Picker.SetHue(x);
        else Picker.SetSaturationValue(x, y);
    }
}
