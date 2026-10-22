using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PuzzleSettingsView : MonoBehaviour
{
    public const float DefaultMenuScale = 1.4f;
    [SerializeField, Range(1f, 1.5f)] private float menuScale = DefaultMenuScale;

    public Canvas Canvas;
    public RectTransform Panel;
    public RawImage Preview;
    public TMP_Text ImageName;
    public TMP_Text VideoName;
    public TMP_Text Status;
    public TMP_Text Count;
    public TMP_InputField ColumnsInput;
    public TMP_InputField RowsInput;
    public Button ColumnsMinus;
    public Button ColumnsPlus;
    public Button RowsMinus;
    public Button RowsPlus;
    public Button PhotoButton;
    public Button VideoButton;
    public Button ClassicButton;
    public Button TilesButton;
    public Toggle PreviewToggle;
    public Toggle LockPlacedToggle;
    public Button ApplyButton;
    public Button CloseButton;
    public Button ExitButton;
    public TMP_Dropdown DisplayModeDropdown;
    public TMP_Dropdown PieceSizeDropdown;
    public GameObject Confirmation;
    public RectTransform ConfirmationPanel;
    public Button ConfirmButton;
    public Button CancelButton;
    public Button[] PaletteButtons;
    public Image[] PaletteMarkers;
    public Color[] PaletteColors;
    public Button CustomColorButton;
    public Image ColorSample;
    public TMP_Text ColorHex;
    public PuzzleColorPicker ColorPicker;

    private static readonly Color Accent = new(0.83f, 0.69f, 0.46f);
    private static readonly Color Surface = new(0.20f, 0.22f, 0.25f);
    private PuzzlePieceMode mode;
    private Color background;
    private Action<Color> backgroundChanged;
    private bool bound;
    private PuzzleDisplaySettings displaySettings;
    public static readonly string[] PieceSizeLabels = { "Динамические", "Оригинальные (1:1)" };

    public int Columns => ReadGrid(ColumnsInput);
    public int Rows => ReadGrid(RowsInput);

    public void ConfigureScale(float scale) => menuScale = Mathf.Clamp(scale, 1f, 1.5f);

    public void Bind(Action photo, Action video, Action apply, Action close, Action confirm,
        Action cancel, Action<PuzzlePieceMode> formChanged, Action<Color> colorChanged,
        Action<bool> previewChanged, Action<bool> lockPlacedChanged, Action<PuzzlePieceSizeMode> sizeChanged, Action quit)
    {
        if (bound) return;
        bound = true;
        backgroundChanged = colorChanged;
        PhotoButton.onClick.AddListener(() => photo());
        VideoButton.onClick.AddListener(() => video());
        ApplyButton.onClick.AddListener(() => { NormalizeGrid(); apply(); });
        CloseButton.onClick.AddListener(() => close());
        ExitButton.onClick.AddListener(() => quit());
        ConfirmButton.onClick.AddListener(() => confirm());
        CancelButton.onClick.AddListener(() => cancel());
        ClassicButton.onClick.AddListener(() => { SetMode(PuzzlePieceMode.Classic); formChanged(mode); });
        TilesButton.onClick.AddListener(() => { SetMode(PuzzlePieceMode.Tiles); formChanged(mode); });
        PreviewToggle.onValueChanged.AddListener(value => previewChanged(value));
        LockPlacedToggle.onValueChanged.AddListener(value => lockPlacedChanged(value));
        ColumnsMinus.onClick.AddListener(() => Step(ColumnsInput, -1));
        ColumnsPlus.onClick.AddListener(() => Step(ColumnsInput, 1));
        RowsMinus.onClick.AddListener(() => Step(RowsInput, -1));
        RowsPlus.onClick.AddListener(() => Step(RowsInput, 1));
        ColumnsInput.onEndEdit.AddListener(_ => NormalizeGrid());
        RowsInput.onEndEdit.AddListener(_ => NormalizeGrid());
        for (var i = 0; i < PaletteButtons.Length; i++)
        {
            var index = i;
            PaletteButtons[i].onClick.AddListener(() => ChooseBackground(PaletteColors[index]));
        }
        ColorPicker.Initialize(ChooseBackground);
        CustomColorButton.onClick.AddListener(() => { ColorPicker.Open(background); Fit(); });
        displaySettings = new PuzzleDisplaySettings();
        if (DisplayModeDropdown != null)
        {
            DisplayModeDropdown.ClearOptions();
            DisplayModeDropdown.AddOptions(new System.Collections.Generic.List<string>(PuzzleDisplaySettings.Labels));
            RefreshDisplayMode();
            DisplayModeDropdown.onValueChanged.AddListener(index => displaySettings.Apply((PuzzleDisplayMode)index));
        }
        if (PieceSizeDropdown != null)
        {
            PieceSizeDropdown.ClearOptions();
            PieceSizeDropdown.AddOptions(new System.Collections.Generic.List<string>(PieceSizeLabels));
            PieceSizeDropdown.onValueChanged.AddListener(index => sizeChanged((PuzzlePieceSizeMode)index));
        }
    }

    public void SetPending(int columns, int rows, PuzzlePieceMode form, Color color)
    {
        ColumnsInput.SetTextWithoutNotify(Mathf.Clamp(columns, 2, 8).ToString());
        RowsInput.SetTextWithoutNotify(Mathf.Clamp(rows, 2, 8).ToString());
        SetMode(form);
        SetBackground(color);
        SetStatus(string.Empty);
        NormalizeGrid();
    }

    public void SetGameplayPreferences(bool preview, bool lockPlaced, PuzzlePieceSizeMode sizeMode)
    {
        PreviewToggle.SetIsOnWithoutNotify(preview);
        LockPlacedToggle.SetIsOnWithoutNotify(lockPlaced);
        SetPieceSizeMode(sizeMode);
        RefreshDisplayMode();
    }

    public void SetPieceSizeMode(PuzzlePieceSizeMode mode)
    {
        if (PieceSizeDropdown != null) PieceSizeDropdown.SetValueWithoutNotify((int)mode);
    }

    public void RefreshDisplayMode()
    {
        if (DisplayModeDropdown != null && displaySettings != null)
            DisplayModeDropdown.SetValueWithoutNotify((int)displaySettings.CurrentMode);
    }

    public void RefreshMedia(PuzzleMediaEntry media)
    {
        ImageName.text = "Фото: " + (media != null ? media.DisplayName : "выбрать");
        var videoName = media?.VideoClip != null ? media.VideoClip.name :
            !string.IsNullOrWhiteSpace(media?.CustomVideoPath) ? Path.GetFileName(media.CustomVideoPath) : "выбрать";
        VideoName.text = "Видео: " + videoName;
        Preview.texture = media?.Image;
        Preview.enabled = Preview.texture != null;
        if (!Preview.enabled) return;
        var area = ((RectTransform)Preview.transform.parent).rect.size;
        var scale = Mathf.Min(area.x / Preview.texture.width, area.y / Preview.texture.height);
        Preview.rectTransform.sizeDelta = new Vector2(Preview.texture.width * scale, Preview.texture.height * scale);
    }

    public void SetMode(PuzzlePieceMode form)
    {
        mode = form;
        StyleChoice(ClassicButton, mode == PuzzlePieceMode.Classic);
        StyleChoice(TilesButton, mode == PuzzlePieceMode.Tiles);
    }

    public void SetBackground(Color color)
    {
        background = color;
        ColorSample.color = color;
        ColorHex.text = "#" + ColorUtility.ToHtmlStringRGB(color);
        for (var i = 0; i < PaletteMarkers.Length; i++)
            PaletteMarkers[i].enabled = ((Vector4)(PaletteColors[i] - color)).sqrMagnitude < 0.0001f;
    }

    private void ChooseBackground(Color color)
    {
        color.a = 1f;
        SetBackground(color);
        backgroundChanged?.Invoke(color);
    }

    public void SetStatus(string message, bool error = false)
    {
        Status.text = message;
        Status.color = error ? new Color(1f, 0.54f, 0.47f) : new Color(0.72f, 0.75f, 0.80f);
        Status.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
    }

    public void ShowConfirmation(bool visible) => Confirmation.SetActive(visible);

    public bool CloseTransient()
    {
        if (PieceSizeDropdown != null && PieceSizeDropdown.IsExpanded)
        {
            PieceSizeDropdown.Hide();
            return true;
        }
        if (DisplayModeDropdown != null && DisplayModeDropdown.IsExpanded)
        {
            DisplayModeDropdown.Hide();
            return true;
        }
        if (!ColorPicker.IsOpen) return false;
        ColorPicker.Close();
        return true;
    }

    public void Fit()
    {
        Canvas.ForceUpdateCanvases();
        var available = ((RectTransform)Canvas.transform).rect.size - Vector2.one * 40f;
        FitPanel(Panel, available, menuScale);
        FitPanel(ConfirmationPanel, available, menuScale);
        FitPanel(ColorPicker.Panel, available, menuScale);
    }

    private static void FitPanel(RectTransform panel, Vector2 available, float preferredScale)
    {
        var size = panel.rect.size;
        var scale = Mathf.Min(preferredScale, available.x / Mathf.Max(1f, size.x), available.y / Mathf.Max(1f, size.y));
        panel.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }

    private void Step(TMP_InputField input, int delta)
    {
        input.SetTextWithoutNotify(Mathf.Clamp(ReadGrid(input) + delta, 2, 8).ToString());
        NormalizeGrid();
    }

    public void NormalizeGrid()
    {
        ColumnsInput.SetTextWithoutNotify(Columns.ToString());
        RowsInput.SetTextWithoutNotify(Rows.ToString());
        ColumnsMinus.interactable = Columns > 2;
        ColumnsPlus.interactable = Columns < 8;
        RowsMinus.interactable = Rows > 2;
        RowsPlus.interactable = Rows < 8;
        Count.text = "Всего деталей: " + Columns * Rows;
    }

    private static int ReadGrid(TMP_InputField input) =>
        int.TryParse(input.text, out var value) ? Mathf.Clamp(value, 2, 8) : 4;

    private static void StyleChoice(Button button, bool selected)
    {
        button.targetGraphic.color = selected ? Accent : Surface;
        button.GetComponentInChildren<TMP_Text>().color = selected ? new Color(0.12f, 0.10f, 0.07f) : Color.white;
    }
}
