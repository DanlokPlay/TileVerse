#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

internal static class PuzzleSceneInstaller
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private static readonly Color Panel = new(0.105f, 0.115f, 0.135f);
    private static readonly Color Surface = new(0.20f, 0.22f, 0.25f);
    private static readonly Color Ink = new(0.94f, 0.95f, 0.97f);
    private static readonly Color Muted = new(0.65f, 0.69f, 0.75f);
    private static readonly Color Accent = new(0.83f, 0.69f, 0.46f);
    private static TMP_FontAsset font;
    private static Sprite rounded;
    private static bool importingResources;

    [MenuItem("Tools/TileVerse/Build editable puzzle gallery scene")]
    public static void InstallCurrentScene()
    {
        if (!PrepareResources())
        {
            EditorApplication.delayCall += InstallCurrentScene;
            return;
        }
        EnsureFont();
        PrepareDefaultPhoto();
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<PuzzleGameController>();
        if (controller == null) throw new System.InvalidOperationException("Puzzle controller is missing.");
        var entries = controller.CopyConfiguredMedia();
        controller.ConfigurePiecePresentation(PuzzleGameController.DefaultSidePieceScale);
        var defaultMedia = PuzzleMediaEntry.LoadDefault();
        if (defaultMedia == null || defaultMedia.VideoClip == null)
            throw new System.InvalidOperationException("The default ispu_1 photo/video pair is missing.");
        entries.RemoveAll(entry => entry.DisplayName == "Горы" || entry.DisplayName == "Поляна" || entry.Image == defaultMedia.Image);
        entries.Insert(0, defaultMedia);
        var scene = SceneManager.GetActiveScene();
        Directory.CreateDirectory("Temp");
        EditorSceneManager.SaveScene(scene, "Temp/TileVerse-before-clean-menu.unity", true);

        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(canvas.gameObject);

        var game = CreateCanvas("Gameplay Canvas", 0);
        var root = (RectTransform)game.transform;
        var background = CreateImage("Background", root, Color.black);
        Stretch(background.rectTransform);
        var left = CreateRect("Left Pieces Area", root);
        var right = CreateRect("Right Pieces Area", root);
        var stage = CreateRect("Board Stage", root);
        Center(stage, new Vector2(1000f, 700f));
        var content = CreateRect("Picture Content", stage);
        content.gameObject.AddComponent<RectMask2D>();
        var guide = CreateRawImage("Puzzle Guide", content, false);
        Stretch(guide.rectTransform);
        guide.color = new Color(1f, 1f, 1f, 0.14f);

        var presentation = CreateRect("Completion Video Presentation", content);
        Stretch(presentation);
        var matte = CreateImage("Video Matte", presentation, Color.black);
        Stretch(matte.rectTransform);
        var overlay = CreateRawImage("Completion Video Overlay", presentation, false);
        Stretch(overlay.rectTransform);
        var overlayGroup = presentation.gameObject.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        overlay.enabled = false;
        var message = Rounded("Completion Message", content, new Color(0.04f, 0.05f, 0.06f, 0.94f));
        message.rectTransform.anchorMin = Vector2.zero;
        message.rectTransform.anchorMax = new Vector2(1f, 0f);
        message.rectTransform.pivot = new Vector2(0.5f, 0f);
        message.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        message.rectTransform.sizeDelta = new Vector2(-24f, 60f);
        var status = Text("Completion Status", message.transform, "", 18f);
        Stretch(status.rectTransform, 8f);
        status.textWrappingMode = TextWrappingModes.Normal;
        message.gameObject.SetActive(false);

        var frameImage = CreateImage("Painting Frame", stage, Color.white);
        frameImage.sprite = LoadMasterFrame();
        frameImage.type = Image.Type.Tiled;
        frameImage.fillCenter = false;
        var ornament = GetOrAdd<PuzzleOrnateFrameView>(controller.gameObject);
        ornament.Configure(frameImage);
        var frame = GetOrAdd<PuzzleFrameView>(controller.gameObject);
        frame.Configure(stage, content, ornament);
        var layout = GetOrAdd<PuzzleBoardLayout>(controller.gameObject);
        layout.Configure(root, stage, left, right, frame);
        var pieces = CreateRect("Pieces Layer", root);
        Stretch(pieces);

        var playerObject = controller.transform.Find("Completion Video Player")?.gameObject;
        if (playerObject == null)
        {
            playerObject = new GameObject("Completion Video Player");
            playerObject.transform.SetParent(controller.transform, false);
        }
        var player = GetOrAdd<VideoPlayer>(playerObject);
        var audio = GetOrAdd<AudioSource>(playerObject);
        player.playOnAwake = false; audio.playOnAwake = false; audio.spatialBlend = 0f;
        var completion = GetOrAdd<PuzzleCompletionVideo>(controller.gameObject);
        completion.Configure(player, audio, overlay, overlayGroup, status, message.gameObject);

        var settingsCanvas = CreateCanvas("Settings Canvas", 20);
        var settings = GetOrAdd<PuzzleSettingsView>(controller.gameObject);
        BuildSettings(settings, settingsCanvas);
        var theme = GetOrAdd<PuzzleThemeView>(controller.gameObject);
        theme.Configure(background);
        controller.ConfigureScene(game, root, pieces, guide.rectTransform, guide, left, right, layout, frame, completion, theme, settings, entries);
        var preview = entries.Count > 0 ? entries[0].Image : null;
        var aspect = preview != null ? preview.width / (float)preview.height : 1.5f;
        var size = new Vector2(850f, 850f / aspect);
        if (size.y > 760f) size = new Vector2(760f * aspect, 760f);
        frame.Apply(PuzzleFrameStyle.Default, size);
        guide.texture = preview;
        settings.SetPending(4, 4, PuzzlePieceMode.Classic, Color.black);
        settings.SetGameplayPreferences(controller.PreviewEnabled, controller.LockPlacedPieces);
        settings.RefreshMedia(entries.Count > 0 ? entries[0] : null);
        settings.Fit();
        settings.Canvas.gameObject.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(settings);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("TileVerse: clean frame and compact TMP settings saved.");
    }

    private static void BuildSettings(PuzzleSettingsView view, Canvas canvas)
    {
        view.ConfigureScale(PuzzleSettingsView.DefaultMenuScale);
        view.Canvas = canvas;
        var root = (RectTransform)canvas.transform;
        var dim = CreateImage("Dimmer", root, new Color(0f, 0f, 0f, 0.65f), true);
        Stretch(dim.rectTransform);
        var panel = Rounded("Settings Panel", root, Panel, true);
        Center(panel.rectTransform, new Vector2(620f, 760f));
        AddShadow(panel);
        view.Panel = panel.rectTransform;
        var title = Text("Title", panel.transform, "Настройки", 28f, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold;
        Top(title.rectTransform, 28f, 40f, 524f);
        view.CloseButton = Button("Close", panel.transform, "×", Surface);
        Top((RectTransform)view.CloseButton.transform, 24f, 40f, 40f, 242f);

        var previewArea = CreateRect("Photo Preview Area", panel.transform);
        Top(previewArea, 88f, 112f, 142f, -191f);
        view.Preview = CreateRawImage("Photo Preview", previewArea, false);
        Center(view.Preview.rectTransform, new Vector2(142f, 112f));
        view.PhotoButton = Button("Choose Photo", panel.transform, "Фото: выбрать", Surface);
        Top((RectTransform)view.PhotoButton.transform, 88f, 48f, 350f, 87f);
        view.ImageName = view.PhotoButton.GetComponentInChildren<TMP_Text>();
        PadLabel(view.ImageName, TextAlignmentOptions.MidlineLeft, 16f);
        view.VideoButton = Button("Choose Video", panel.transform, "Видео: выбрать", Surface);
        Top((RectTransform)view.VideoButton.transform, 152f, 48f, 350f, 87f);
        view.VideoName = view.VideoButton.GetComponentInChildren<TMP_Text>();
        PadLabel(view.VideoName, TextAlignmentOptions.MidlineLeft, 16f);

        Label("Form Label", panel.transform, "Форма деталей", 222f);
        view.ClassicButton = Button("Classic Form", panel.transform, "Пазлы", Surface);
        Top((RectTransform)view.ClassicButton.transform, 256f, 44f, 252f, -136f);
        view.TilesButton = Button("Tile Form", panel.transform, "Плитки", Surface);
        Top((RectTransform)view.TilesButton.transform, 256f, 44f, 252f, 136f);

        view.PreviewToggle = Checkbox("Preview Toggle", panel.transform, "Предосмотр");
        Top((RectTransform)view.PreviewToggle.transform, 320f, 34f, 252f, -136f);
        view.LockPlacedToggle = Checkbox("Placement Lock Toggle", panel.transform, "Закреплять в рамке");
        Top((RectTransform)view.LockPlacedToggle.transform, 320f, 34f, 252f, 136f);

        var colLabel = Text("Columns Label", panel.transform, "Столбцы", 16f, TextAlignmentOptions.MidlineLeft);
        colLabel.color = Muted; Top(colLabel.rectTransform, 374f, 24f, 244f, -136f);
        var rowLabel = Text("Rows Label", panel.transform, "Строки", 16f, TextAlignmentOptions.MidlineLeft);
        rowLabel.color = Muted; Top(rowLabel.rectTransform, 374f, 24f, 244f, 136f);
        view.ColumnsMinus = Button("Columns Minus", panel.transform, "−", Surface);
        view.ColumnsPlus = Button("Columns Plus", panel.transform, "+", Surface);
        view.RowsMinus = Button("Rows Minus", panel.transform, "−", Surface);
        view.RowsPlus = Button("Rows Plus", panel.transform, "+", Surface);
        Top((RectTransform)view.ColumnsMinus.transform, 407f, 44f, 52f, -232f);
        Top((RectTransform)view.ColumnsPlus.transform, 407f, 44f, 52f, -40f);
        Top((RectTransform)view.RowsMinus.transform, 407f, 44f, 52f, 40f);
        Top((RectTransform)view.RowsPlus.transform, 407f, 44f, 52f, 232f);
        view.ColumnsInput = Input("Columns Input", panel.transform, true);
        view.RowsInput = Input("Rows Input", panel.transform, true);
        Top((RectTransform)view.ColumnsInput.transform, 407f, 44f, 100f, -136f);
        Top((RectTransform)view.RowsInput.transform, 407f, 44f, 100f, 136f);
        view.Count = Text("Piece Count", panel.transform, "Всего деталей: 16", 15f, TextAlignmentOptions.MidlineLeft);
        view.Count.color = Muted; Top(view.Count.rectTransform, 464f, 24f, 524f);

        Label("Background Label", panel.transform, "Фон", 502f);
        var hex = new[] { "#000000", "#20232A", "#354759", "#263D35", "#879B88", "#C3B394", "#EAE3D6", "#FFFFFF", "#A87680", "#8C83A4" };
        view.PaletteButtons = new Button[hex.Length];
        view.PaletteMarkers = new Image[hex.Length];
        view.PaletteColors = new Color[hex.Length];
        for (var i = 0; i < hex.Length; i++)
        {
            ColorUtility.TryParseHtmlString(hex[i], out var color);
            view.PaletteColors[i] = color;
            var button = Button("Palette " + hex[i], panel.transform, "", color);
            Top((RectTransform)button.transform, 534f, 40f, 40f, -234f + i * 52f);
            view.PaletteButtons[i] = button;
            var marker = CreateImage("Selection", button.transform, Accent);
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            marker.rectTransform.sizeDelta = new Vector2(20f, 2f);
            marker.rectTransform.anchoredPosition = new Vector2(0f, -6f);
            view.PaletteMarkers[i] = marker;
        }
        view.CustomColorButton = Button("Custom Color", panel.transform, "Свой цвет", Surface);
        Top((RectTransform)view.CustomColorButton.transform, 590f, 44f, 524f);
        PadLabel(view.CustomColorButton.GetComponentInChildren<TMP_Text>(), TextAlignmentOptions.MidlineLeft, 16f);
        view.ColorHex = Text("Current Hex", view.CustomColorButton.transform, "#000000", 16f, TextAlignmentOptions.MidlineRight);
        Center(view.ColorHex.rectTransform, new Vector2(130f, 32f));
        view.ColorHex.rectTransform.anchoredPosition = new Vector2(150f, 0f);
        view.ColorHex.color = Muted;
        view.ColorSample = Rounded("Current Color", view.CustomColorButton.transform, Color.black);
        Center(view.ColorSample.rectTransform, Vector2.one * 24f);
        view.ColorSample.rectTransform.anchoredPosition = new Vector2(232f, 0f);
        view.Status = Text("Status", panel.transform, "", 14f, TextAlignmentOptions.MidlineLeft);
        Top(view.Status.rectTransform, 644f, 20f, 524f);
        view.Status.gameObject.SetActive(false);
        view.ApplyButton = Button("Apply", panel.transform, "Применить", Accent);
        view.ApplyButton.GetComponentInChildren<TMP_Text>().color = new Color(0.12f, 0.1f, 0.07f);
        Bottom((RectTransform)view.ApplyButton.transform, 24f, 48f, 350f, -87f);
        view.ExitButton = Button("Exit Application", panel.transform, "Выход", Surface);
        Bottom((RectTransform)view.ExitButton.transform, 24f, 48f, 158f, 183f);

        var confirmation = CreateImage("Reset Confirmation Dialog", root, new Color(0f, 0f, 0f, 0.72f), true);
        Stretch(confirmation.rectTransform);
        view.Confirmation = confirmation.gameObject;
        var confirmPanel = Rounded("Confirmation Panel", confirmation.transform, Panel, true);
        Center(confirmPanel.rectTransform, new Vector2(500f, 260f));
        view.ConfirmationPanel = confirmPanel.rectTransform;
        var question = Text("Question", confirmPanel.transform, "Начать заново?", 24f);
        question.fontStyle = FontStyles.Bold; Top(question.rectTransform, 30f, 36f, 428f);
        var explanation = Text("Explanation", confirmPanel.transform, "Новые настройки сбросят текущую сборку.", 17f);
        explanation.textWrappingMode = TextWrappingModes.Normal; Top(explanation.rectTransform, 84f, 64f, 400f);
        view.CancelButton = Button("Cancel Reset", confirmPanel.transform, "Отмена", Surface);
        Bottom((RectTransform)view.CancelButton.transform, 30f, 46f, 204f, -112f);
        view.ConfirmButton = Button("Confirm Reset", confirmPanel.transform, "Начать заново", Accent);
        view.ConfirmButton.GetComponentInChildren<TMP_Text>().color = new Color(0.12f, 0.1f, 0.07f);
        Bottom((RectTransform)view.ConfirmButton.transform, 30f, 46f, 204f, 112f);
        confirmation.gameObject.SetActive(false);
        view.ColorPicker = BuildPicker(root);
    }

    private static PuzzleColorPicker BuildPicker(RectTransform root)
    {
        var dim = CreateImage("Color Picker", root, new Color(0f, 0f, 0f, 0.74f), true);
        Stretch(dim.rectTransform);
        var picker = dim.gameObject.AddComponent<PuzzleColorPicker>();
        var panel = Rounded("Color Panel", dim.transform, Panel, true);
        Center(panel.rectTransform, new Vector2(380f, 476f));
        picker.Panel = panel.rectTransform;
        var title = Text("Title", panel.transform, "Цвет фона", 24f, TextAlignmentOptions.MidlineLeft);
        title.fontStyle = FontStyles.Bold; Top(title.rectTransform, 24f, 36f, 316f);
        picker.CloseButton = Button("Close Picker", panel.transform, "×", Surface);
        Top((RectTransform)picker.CloseButton.transform, 22f, 34f, 34f, 141f);
        picker.SaturationValue = CreateRawImage("Saturation Value", panel.transform, true);
        Top(picker.SaturationValue.rectTransform, 78f, 206f, 316f);
        var svSurface = picker.SaturationValue.gameObject.AddComponent<PuzzleColorSurface>();
        svSurface.Picker = picker;
        var cursor = Rounded("Color Cursor", picker.SaturationValue.transform, Color.white);
        Center(cursor.rectTransform, Vector2.one * 16f);
        cursor.fillCenter = false; cursor.pixelsPerUnitMultiplier = 4f;
        picker.ColorCursor = cursor.rectTransform;
        picker.HueBar = CreateRawImage("Hue", panel.transform, true);
        Top(picker.HueBar.rectTransform, 304f, 18f, 316f);
        var hueSurface = picker.HueBar.gameObject.AddComponent<PuzzleColorSurface>();
        hueSurface.Picker = picker; hueSurface.IsHue = true;
        var hueCursor = CreateImage("Hue Cursor", picker.HueBar.transform, Color.white);
        Center(hueCursor.rectTransform, new Vector2(3f, 26f));
        picker.HueCursor = hueCursor.rectTransform;
        picker.Preview = Rounded("Color Preview", panel.transform, Color.black);
        Top(picker.Preview.rectTransform, 344f, 42f, 42f, -137f);
        picker.HexInput = Input("Hex Input", panel.transform, false);
        Top((RectTransform)picker.HexInput.transform, 344f, 42f, 258f, 29f);
        picker.HexInput.characterLimit = 7;
        picker.HexInput.text = "#000000";
        picker.Error = Text("Error", panel.transform, "", 12f);
        picker.Error.color = new Color(1f, 0.54f, 0.47f);
        Top(picker.Error.rectTransform, 394f, 16f, 316f);
        picker.Error.gameObject.SetActive(false);
        picker.DoneButton = Button("Done", panel.transform, "Готово", Accent);
        picker.DoneButton.GetComponentInChildren<TMP_Text>().color = new Color(0.12f, 0.10f, 0.07f);
        Bottom((RectTransform)picker.DoneButton.transform, 22f, 42f, 316f);
        dim.gameObject.SetActive(false);
        return picker;
    }

    public static bool PrepareResources()
    {
        if (Resources.Load<TMP_Settings>("TMP Settings") != null &&
            AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf") != null) return true;
        if (!importingResources)
        {
            importingResources = true;
            var packages = Directory.GetDirectories("Library/PackageCache", "com.unity.ugui@*");
            AssetDatabase.ImportPackage(Path.Combine(packages[0], "Package Resources/TMP Essential Resources.unitypackage"), false);
        }
        return false;
    }

    private static void EnsureFont()
    {
        const string assetPath = "Assets/Art/UI/GallerySans.asset";
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (font == null)
        {
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/TextMesh Pro/Fonts/LiberationSans.ttf");
            font = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            font.name = "GallerySans";
            var characters = new List<uint>();
            for (uint c = 32; c < 127; c++) characters.Add(c);
            for (uint c = 0x400; c < 0x460; c++) characters.Add(c);
            characters.AddRange(new uint[] { 0xD7, 0x2013, 0x2014, 0x2026, 0x2212 });
            font.TryAddCharacters(characters.ToArray(), out _);
            AssetDatabase.CreateAsset(font, assetPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
            EditorUtility.SetDirty(font);
        }
        var settings = new SerializedObject(Resources.Load<TMP_Settings>("TMP Settings"));
        settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
        settings.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        rounded = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/GalleryRounded.png");
    }

    private static Sprite LoadMasterFrame()
    {
        const string path = "Assets/Art/Frames/Coherent/OrnateGoldMaster.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(textureSettings);
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = Vector4.one * 320f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void PrepareDefaultPhoto()
    {
        var importer = AssetImporter.GetAtPath("Assets/Resources/ispu_1.png") as TextureImporter;
        if (importer == null) return;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycast = false)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = raycast;
        return image;
    }

    private static Image Rounded(string name, Transform parent, Color color, bool raycast = false)
    {
        var image = CreateImage(name, parent, color, raycast);
        image.sprite = rounded; image.type = Image.Type.Sliced;
        return image;
    }

    private static void AddShadow(Image image)
    {
        var shadow = image.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        shadow.effectDistance = new Vector2(0f, -8f);
    }

    private static RawImage CreateRawImage(string name, Transform parent, bool raycast)
    {
        var rect = CreateRect(name, parent);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.raycastTarget = raycast;
        return image;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, string value, float size,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var rect = CreateRect(name, parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font; text.text = value; text.fontSize = size;
        text.alignment = alignment; text.color = Ink; text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap; text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static void Label(string name, Transform parent, string value, float top)
    {
        var label = Text(name, parent, value, 16f, TextAlignmentOptions.MidlineLeft);
        label.color = Muted; Top(label.rectTransform, top, 24f, 524f);
    }

    private static Button Button(string name, Transform parent, string label, Color color)
    {
        var image = Rounded(name, parent, color, true);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        colors.fadeDuration = 0.1f; button.colors = colors;
        var text = Text("Label", button.transform, label, 18f);
        Stretch(text.rectTransform, label == "×" ? 2f : 8f);
        return button;
    }

    private static Toggle Checkbox(string name, Transform parent, string label)
    {
        var hitArea = CreateImage(name, parent, Color.clear, true);
        var toggle = hitArea.gameObject.AddComponent<Toggle>();
        var box = Rounded("Box", hitArea.transform, Surface);
        box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        box.rectTransform.anchoredPosition = new Vector2(14f, 0f);
        box.rectTransform.sizeDelta = Vector2.one * 28f;
        var mark = CreateImage("Checkmark", box.transform, Accent);
        mark.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
        Center(mark.rectTransform, Vector2.one * 24f);
        toggle.targetGraphic = box;
        toggle.graphic = mark;
        toggle.isOn = true;
        var text = Text("Label", hitArea.transform, label, 18f, TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(40f, 0f);
        return toggle;
    }

    private static TMP_InputField Input(string name, Transform parent, bool numeric)
    {
        var image = Rounded(name, parent, new Color(0.07f, 0.08f, 0.10f), true);
        var input = image.gameObject.AddComponent<TMP_InputField>();
        var viewport = CreateRect("Text Area", image.transform);
        Stretch(viewport, 8f); viewport.gameObject.AddComponent<RectMask2D>();
        var text = Text("Text", viewport, "", 20f, numeric ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft);
        Stretch(text.rectTransform);
        input.textViewport = viewport; input.textComponent = text; input.targetGraphic = image;
        input.contentType = numeric ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = numeric ? 2 : 7;
        input.customCaretColor = true; input.caretColor = Color.white;
        input.selectionColor = new Color(Accent.r, Accent.g, Accent.b, 0.35f);
        input.text = numeric ? "4" : "";
        return input;
    }

    private static void PadLabel(TMP_Text text, TextAlignmentOptions alignment, float padding)
    {
        text.alignment = alignment;
        text.margin = new Vector4(padding - 8f, 0f, padding - 8f, 0f);
    }

    private static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset; rect.offsetMax = -Vector2.one * inset;
    }

    private static void Center(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = size;
    }

    private static void Top(RectTransform rect, float top, float height, float width, float x = 0f)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(x, -top); rect.sizeDelta = new Vector2(width, height);
    }

    private static void Bottom(RectTransform rect, float bottom, float height, float width, float x = 0f)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, bottom); rect.sizeDelta = new Vector2(width, height);
    }
}
#endif
