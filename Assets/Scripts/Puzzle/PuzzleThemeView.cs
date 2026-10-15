using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleThemeView : MonoBehaviour
{
    [SerializeField] private Image[] surfaces;

    public void Configure(params Image[] backgrounds) => surfaces = backgrounds;

    public void Apply(Color color)
    {
        color.a = 1f;
        if (surfaces == null) return;
        foreach (var surface in surfaces)
            if (surface != null) surface.color = color;
    }
}
