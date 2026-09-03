using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleThemeView : MonoBehaviour
{
    [SerializeField] private Image[] surfaces;
    [SerializeField] private Image[] panels;
    [SerializeField] private Text[] texts;
    [SerializeField] private Color lightSurface = new(0.93f, 0.91f, 0.85f, 1f);
    [SerializeField] private Color darkSurface = new(0.08f, 0.09f, 0.11f, 1f);
    [SerializeField] private Color lightPanel = new(0.98f, 0.96f, 0.90f, 0.97f);
    [SerializeField] private Color darkPanel = new(0.15f, 0.16f, 0.19f, 0.97f);
    [SerializeField] private Color lightText = new(0.13f, 0.11f, 0.07f, 1f);
    [SerializeField] private Color darkText = new(0.95f, 0.92f, 0.84f, 1f);

    public void Configure(Image[] backgroundSurfaces, Image[] panelSurfaces, Text[] themedTexts)
    {
        surfaces = backgroundSurfaces;
        panels = panelSurfaces;
        texts = themedTexts;
    }

    public void Apply(bool dark)
    {
        ApplyColor(surfaces, dark ? darkSurface : lightSurface);
        ApplyColor(panels, dark ? darkPanel : lightPanel);

        if (texts == null)
        {
            return;
        }

        var color = dark ? darkText : lightText;
        foreach (var text in texts)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }

    private static void ApplyColor(Image[] images, Color color)
    {
        if (images == null)
        {
            return;
        }

        foreach (var image in images)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}
