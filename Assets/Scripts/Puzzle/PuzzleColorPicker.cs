using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleColorPicker : MonoBehaviour
{
    public RectTransform Panel;
    public RawImage SaturationValue;
    public RawImage HueBar;
    public RectTransform ColorCursor;
    public RectTransform HueCursor;
    public TMP_InputField HexInput;
    public Image Preview;
    public TMP_Text Error;
    public Button DoneButton;
    public Button CloseButton;

    private Action<Color> changed;
    private Texture2D squareTexture;
    private Texture2D hueTexture;
    private float hue;
    private float saturation;
    private float value;
    private bool initialized;

    public bool IsOpen => gameObject.activeSelf;
    public Color SelectedColor => Color.HSVToRGB(hue, saturation, value);

    public void Initialize(Action<Color> onChange)
    {
        changed = onChange;
        if (initialized) return;
        initialized = true;
        DoneButton.onClick.AddListener(Close);
        CloseButton.onClick.AddListener(Close);
        HexInput.onEndEdit.AddListener(SubmitHex);
    }

    public void Open(Color color)
    {
        gameObject.SetActive(true);
        EnsureTextures();
        Color.RGBToHSV(color, out hue, out saturation, out value);
        RefreshSquare();
        RefreshControls();
    }

    public void Close() => gameObject.SetActive(false);

    public void SetSaturationValue(float s, float v)
    {
        saturation = Mathf.Clamp01(s);
        value = Mathf.Clamp01(v);
        Publish();
    }

    public void SetHue(float h)
    {
        hue = Mathf.Clamp01(h);
        RefreshSquare();
        Publish();
    }

    public void SubmitHex(string input)
    {
        var hex = input.Trim().TrimStart('#');
        if (hex.Length != 6 || !ColorUtility.TryParseHtmlString("#" + hex, out var color))
        {
            Error.text = "Введите цвет в формате #RRGGBB";
            Error.gameObject.SetActive(true);
            return;
        }
        Color.RGBToHSV(color, out hue, out saturation, out value);
        RefreshSquare();
        Publish();
    }

    private void Publish()
    {
        RefreshControls();
        changed?.Invoke(SelectedColor);
    }

    private void RefreshControls()
    {
        Preview.color = SelectedColor;
        HexInput.SetTextWithoutNotify("#" + ColorUtility.ToHtmlStringRGB(SelectedColor));
        Error.gameObject.SetActive(false);
        var square = SaturationValue.rectTransform.rect;
        ColorCursor.anchoredPosition = new Vector2((saturation - 0.5f) * square.width, (value - 0.5f) * square.height);
        HueCursor.anchoredPosition = new Vector2((hue - 0.5f) * HueBar.rectTransform.rect.width, 0f);
    }

    private void EnsureTextures()
    {
        if (squareTexture != null) return;
        squareTexture = new Texture2D(128, 128, TextureFormat.RGB24, false) { name = "Picker SV", wrapMode = TextureWrapMode.Clamp };
        hueTexture = new Texture2D(256, 1, TextureFormat.RGB24, false) { name = "Picker Hue", wrapMode = TextureWrapMode.Clamp };
        var colors = new Color[256];
        for (var x = 0; x < 256; x++) colors[x] = Color.HSVToRGB(x / 255f, 1f, 1f);
        hueTexture.SetPixels(colors); hueTexture.Apply();
        SaturationValue.texture = squareTexture;
        HueBar.texture = hueTexture;
    }

    private void RefreshSquare()
    {
        EnsureTextures();
        var colors = new Color[128 * 128];
        var tint = Color.HSVToRGB(hue, 1f, 1f);
        for (var y = 0; y < 128; y++)
        for (var x = 0; x < 128; x++)
        {
            var color = Color.Lerp(Color.white, tint, x / 127f) * (y / 127f);
            color.a = 1f;
            colors[y * 128 + x] = color;
        }
        squareTexture.SetPixels(colors); squareTexture.Apply();
    }

    private void OnDestroy()
    {
        if (squareTexture != null) Destroy(squareTexture);
        if (hueTexture != null) Destroy(hueTexture);
    }
}
