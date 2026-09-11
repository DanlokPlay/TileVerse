#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

internal static class PuzzleSceneInstaller
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string OrnateFrameInstallMarkerPath = "Assets/Editor/TileVerseOrnateFrame.install";
    private static readonly Color Paper = new(0.93f, 0.91f, 0.85f, 1f);
    private static readonly Color Panel = new(0.98f, 0.96f, 0.90f, 0.97f);
    private static readonly Color Ink = new(0.15f, 0.11f, 0.06f, 1f);
    private static readonly Color Gold = new(0.62f, 0.39f, 0.10f, 1f);
    private static Font defaultFont;

    [InitializeOnLoadMethod]
    private static void InstallOrnateFrameWhenRequested()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot) || !File.Exists(Path.Combine(projectRoot, OrnateFrameInstallMarkerPath)))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            var markerPath = Path.Combine(projectRoot, OrnateFrameInstallMarkerPath);
            if (!File.Exists(markerPath))
            {
                return;
            }

            File.Delete(markerPath);
            AssetDatabase.Refresh();
            InstallCurrentScene();
        };
    }

    [MenuItem("Tools/TileVerse/Build editable puzzle gallery scene")]
    public static void InstallCurrentScene()
    {
        if (SceneManager.GetActiveScene().path != SampleScenePath)
        {
            EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        var controller = Object.FindFirstObjectByType<PuzzleGameController>();
        if (controller == null)
        {
            Debug.LogError("PuzzleSceneInstaller: PuzzleGameController was not found in the active scene.");
            return;
        }

        foreach (var existingCanvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(existingCanvas.gameObject);
        }

        var gameplayCanvas = CreateCanvas("Gameplay Canvas", 0);
        var gameplayRoot = gameplayCanvas.GetComponent<RectTransform>();
        var gameBackground = CreateImage("Background", gameplayRoot, Paper, true);
        Stretch(gameBackground.rectTransform, 0f);

        var leftArea = CreateImage("Left Pieces Area", gameplayRoot, new Color(1f, 1f, 1f, 0f), false).rectTransform;
        var rightArea = CreateImage("Right Pieces Area", gameplayRoot, new Color(1f, 1f, 1f, 0f), false).rectTransform;
        var frameTemplate = CreateRect("Board Stage", gameplayRoot);
        frameTemplate.anchorMin = frameTemplate.anchorMax = new Vector2(0.5f, 0.5f);
        frameTemplate.pivot = new Vector2(0.5f, 0.5f);

        var outerFrame = CreateImage("Frame Template", frameTemplate, PuzzleFrameStyle.Default.outerColor, false);
        var innerShadow = CreateImage("Inner Shadow", outerFrame.rectTransform, PuzzleFrameStyle.Default.shadowColor, false);
        var goldTrim = CreateImage("Gold Trim", innerShadow.rectTransform, PuzzleFrameStyle.Default.trimColor, false);
        var mat = CreateImage("Mat", goldTrim.rectTransform, PuzzleFrameStyle.Default.matColor, false);
        var contentRoot = CreateRect("Picture Content", mat.rectTransform);
        contentRoot.gameObject.AddComponent<RectMask2D>();

        var ornamentRoot = CreateRect("Ornate Frame Parts", outerFrame.rectTransform);
        Stretch(ornamentRoot, 0f);
        var topLeftCornerSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldTopLeftCorner.png");
        var topRightCornerSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldTopRightCorner.png");
        var bottomLeftCornerSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldBottomLeftCorner.png");
        var bottomRightCornerSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldBottomRightCorner.png");
        var topEdgeSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldTopEdge.png");
        var bottomEdgeSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldBottomEdge.png");
        var leftEdgeSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldLeftEdge.png");
        var rightEdgeSprite = LoadFrameSprite("Assets/Art/Frames/Coherent/OrnateGoldRightEdge.png");
        var topLeftCorner = CreateFramePart("Top Left Corner", ornamentRoot, topLeftCornerSprite, Image.Type.Simple);
        var topRightCorner = CreateFramePart("Top Right Corner", ornamentRoot, topRightCornerSprite, Image.Type.Simple);
        var bottomLeftCorner = CreateFramePart("Bottom Left Corner", ornamentRoot, bottomLeftCornerSprite, Image.Type.Simple);
        var bottomRightCorner = CreateFramePart("Bottom Right Corner", ornamentRoot, bottomRightCornerSprite, Image.Type.Simple);
        var topEdge = CreateFramePart("Top Edge", ornamentRoot, topEdgeSprite, Image.Type.Simple);
        var bottomEdge = CreateFramePart("Bottom Edge", ornamentRoot, bottomEdgeSprite, Image.Type.Simple);
        var leftEdge = CreateFramePart("Left Edge", ornamentRoot, leftEdgeSprite, Image.Type.Simple);
        var rightEdge = CreateFramePart("Right Edge", ornamentRoot, rightEdgeSprite, Image.Type.Simple);

        var guide = CreateRawImage("Puzzle Guide", contentRoot, new Color(1f, 1f, 1f, 0.13f), false);
        Stretch(guide.rectTransform, 0f);
        var videoOverlay = CreateRawImage("Completion Video Overlay", contentRoot, Color.white, false);
        Stretch(videoOverlay.rectTransform, 0f);
        videoOverlay.enabled = false;
        var overlayGroup = videoOverlay.gameObject.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;
        var completionStatus = CreateText("Completion Status", frameTemplate, string.Empty, 24, TextAnchor.LowerCenter);
        completionStatus.gameObject.SetActive(false);
        completionStatus.rectTransform.anchorMin = new Vector2(0f, 0f);
        completionStatus.rectTransform.anchorMax = new Vector2(1f, 0f);
        completionStatus.rectTransform.pivot = new Vector2(0.5f, 1f);
        completionStatus.rectTransform.anchoredPosition = new Vector2(0f, -12f);
        completionStatus.rectTransform.sizeDelta = new Vector2(-16f, 54f);

        var piecesLayer = CreateRect("Pieces Layer", gameplayRoot);
        Stretch(piecesLayer, 0f);

        var videoPlayerObject = controller.transform.Find("Completion Video Player")?.gameObject;
        if (videoPlayerObject == null)
        {
            videoPlayerObject = new GameObject("Completion Video Player");
            videoPlayerObject.transform.SetParent(controller.transform, false);
        }

        var videoPlayer = GetOrAdd<VideoPlayer>(videoPlayerObject);
        videoPlayer.playOnAwake = false;
        var audioSource = GetOrAdd<AudioSource>(videoPlayerObject);
        audioSource.playOnAwake = false;

        var frameView = GetOrAdd<PuzzleFrameView>(controller.gameObject);
        frameView.Configure(frameTemplate, outerFrame.rectTransform, innerShadow.rectTransform, goldTrim.rectTransform, mat.rectTransform, contentRoot,
            outerFrame, innerShadow, goldTrim, mat);
        var ornateFrame = GetOrAdd<PuzzleOrnateFrameView>(controller.gameObject);
        ornateFrame.Configure(ornamentRoot, topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner, topEdge, bottomEdge, leftEdge, rightEdge);
        ornateFrame.UseCoherentMasterPreset();
        frameView.ConfigureOrnament(ornateFrame);
        var boardLayout = GetOrAdd<PuzzleBoardLayout>(controller.gameObject);
        boardLayout.Configure(gameplayRoot, frameTemplate, leftArea, rightArea, frameView);
        var completionVideo = GetOrAdd<PuzzleCompletionVideo>(controller.gameObject);
        completionVideo.Configure(videoPlayer, audioSource, videoOverlay, overlayGroup, completionStatus);

        var settingsCanvas = CreateCanvas("Settings Canvas", 20);
        var settingsRoot = settingsCanvas.GetComponent<RectTransform>();
        var dim = CreateImage("Dimmer", settingsRoot, new Color(0.05f, 0.04f, 0.025f, 0.76f), true);
        Stretch(dim.rectTransform, 0f);
        var settingsPanel = CreateImage("Settings Panel", settingsRoot, Panel, true);
        SetCentered(settingsPanel.rectTransform, new Vector2(720f, 780f));
        var settingsTitle = CreateText("Title", settingsPanel.rectTransform, "Настройки картины", 36, TextAnchor.UpperCenter);
        SetTop(settingsTitle.rectTransform, 52f, 76f);
        var selectedLabel = CreateText("Selected Artwork", settingsPanel.rectTransform, "Картина", 23, TextAnchor.UpperLeft);
        SetTop(selectedLabel.rectTransform, 142f, 78f, 620f, 0f);

        var builtInA = CreateButton("Artwork A", settingsPanel.rectTransform, "Горы", Gold);
        SetTop(builtInA.GetComponent<RectTransform>(), 235f, 54f, 280f, -160f);
        var builtInB = CreateButton("Artwork B", settingsPanel.rectTransform, "Поляна", Gold);
        SetTop(builtInB.GetComponent<RectTransform>(), 235f, 54f, 280f, 160f);
        var customImage = CreateButton("Choose Photo", settingsPanel.rectTransform, "Выбрать фото", Gold);
        SetTop(customImage.GetComponent<RectTransform>(), 305f, 54f, 280f, -160f);
        var customVideo = CreateButton("Choose Video", settingsPanel.rectTransform, "Выбрать видео", Gold);
        SetTop(customVideo.GetComponent<RectTransform>(), 305f, 54f, 280f, 160f);

        var columnLabel = CreateText("Columns Label", settingsPanel.rectTransform, "Столбцы", 22, TextAnchor.MiddleLeft);
        SetTop(columnLabel.rectTransform, 397f, 38f, 160f, -230f);
        var horizontalSlider = CreateSlider("Columns Slider", settingsPanel.rectTransform);
        SetTop(horizontalSlider.GetComponent<RectTransform>(), 397f, 34f, 270f, 0f);
        var horizontalValue = CreateText("Columns Value", settingsPanel.rectTransform, "4", 23, TextAnchor.MiddleCenter);
        SetTop(horizontalValue.rectTransform, 397f, 38f, 55f, 245f);
        var rowLabel = CreateText("Rows Label", settingsPanel.rectTransform, "Строки", 22, TextAnchor.MiddleLeft);
        SetTop(rowLabel.rectTransform, 455f, 38f, 160f, -230f);
        var verticalSlider = CreateSlider("Rows Slider", settingsPanel.rectTransform);
        SetTop(verticalSlider.GetComponent<RectTransform>(), 455f, 34f, 270f, 0f);
        var verticalValue = CreateText("Rows Value", settingsPanel.rectTransform, "4", 23, TextAnchor.MiddleCenter);
        SetTop(verticalValue.rectTransform, 455f, 38f, 55f, 245f);

        var themeButton = CreateButton("Theme Toggle", settingsPanel.rectTransform, "Тема: светлая", new Color(0.22f, 0.26f, 0.29f, 1f));
        SetTop(themeButton.GetComponent<RectTransform>(), 530f, 52f, 360f, 0f);
        var statusLabel = CreateText("Status", settingsPanel.rectTransform, string.Empty, 18, TextAnchor.MiddleCenter);
        SetTop(statusLabel.rectTransform, 600f, 78f, 620f, 0f);
        var applyButton = CreateButton("Apply", settingsPanel.rectTransform, "Применить", new Color(0.30f, 0.45f, 0.20f, 1f));
        SetBottom(applyButton.GetComponent<RectTransform>(), 72f, 58f, 230f, -135f);
        var closeButton = CreateButton("Close", settingsPanel.rectTransform, "Закрыть", new Color(0.38f, 0.30f, 0.22f, 1f));
        SetBottom(closeButton.GetComponent<RectTransform>(), 72f, 58f, 230f, 135f);

        var confirmation = CreateImage("Reset Confirmation Dialog", settingsRoot, new Color(0.18f, 0.15f, 0.11f, 0.98f), true);
        SetCentered(confirmation.rectTransform, new Vector2(550f, 290f));
        var confirmationTitle = CreateText("Question", confirmation.rectTransform, "Сбросить текущий пазл?", 28, TextAnchor.UpperCenter);
        SetTop(confirmationTitle.rectTransform, 44f, 52f);
        var confirmationText = CreateText("Explanation", confirmation.rectTransform, "Фото, видео или размер сетки изменились. Текущая сборка будет потеряна.", 20, TextAnchor.MiddleCenter);
        SetCentered(confirmationText.rectTransform, new Vector2(450f, 86f));
        var resetConfirm = CreateButton("Confirm Reset", confirmation.rectTransform, "Сбросить", new Color(0.54f, 0.22f, 0.15f, 1f));
        SetBottom(resetConfirm.GetComponent<RectTransform>(), 44f, 54f, 180f, -105f);
        var resetCancel = CreateButton("Cancel Reset", confirmation.rectTransform, "Отмена", new Color(0.42f, 0.37f, 0.30f, 1f));
        SetBottom(resetCancel.GetComponent<RectTransform>(), 44f, 54f, 180f, 105f);
        confirmation.gameObject.SetActive(false);

        var themeView = GetOrAdd<PuzzleThemeView>(controller.gameObject);
        themeView.Configure(new[] { gameBackground }, new[] { settingsPanel, confirmation }, new[]
        {
            settingsTitle, selectedLabel, columnLabel, rowLabel, horizontalValue, verticalValue, statusLabel, completionStatus,
            confirmationTitle, confirmationText
        });

        var mountains = Resources.Load<Texture2D>("Горы");
        var glade = Resources.Load<Texture2D>("Поляна");
        var entries = new List<PuzzleMediaEntry>();
        if (mountains != null) entries.Add(new PuzzleMediaEntry("Горы", mountains));
        if (glade != null) entries.Add(new PuzzleMediaEntry("Поляна", glade));

        controller.ConfigureScene(gameplayCanvas, gameplayRoot, piecesLayer, guide.rectTransform, guide, leftArea, rightArea, boardLayout, frameView, completionVideo, themeView,
            settingsCanvas, selectedLabel, statusLabel, horizontalValue, verticalValue, horizontalSlider, verticalSlider, builtInA, builtInB, customImage, customVideo,
            themeButton, applyButton, closeButton, confirmation.gameObject, resetConfirm, resetCancel, entries);

        ApplyEditorPreviewLayout(frameView, frameTemplate, leftArea, rightArea, mountains ?? glade);

        settingsCanvas.gameObject.SetActive(false);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Selection.activeGameObject = controller.gameObject;
        Debug.Log("TileVerse: editable puzzle gallery scene has been created.");
    }

    private static Canvas CreateCanvas(string name, int sortingOrder)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component => target.TryGetComponent<T>(out var component) ? component : target.AddComponent<T>();

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static RawImage CreateRawImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<RawImage>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    private static Image CreateFramePart(string name, Transform parent, Sprite sprite, Image.Type type)
    {
        var image = CreateImage(name, parent, Color.white, false);
        image.sprite = sprite;
        image.type = type;
        image.preserveAspect = false;
        return image;
    }

    private static Sprite LoadFrameSprite(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            var mustReimport = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               !importer.alphaIsTransparency ||
                               importer.mipmapEnabled ||
                                importer.wrapMode != TextureWrapMode.Clamp ||
                               importer.spritePixelsPerUnit != 512f ||
                               importer.maxTextureSize != 2048;
            if (mustReimport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.spritePixelsPerUnit = 512f;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void ApplyEditorPreviewLayout(PuzzleFrameView frameView, RectTransform frameTemplate, RectTransform leftArea, RectTransform rightArea, Texture2D previewTexture)
    {
        if (frameView == null || frameTemplate == null)
        {
            return;
        }

        var previewContent = FitPreviewContent(previewTexture);
        frameTemplate.anchorMin = frameTemplate.anchorMax = new Vector2(0.5f, 0.5f);
        frameTemplate.pivot = new Vector2(0.5f, 0.5f);
        frameTemplate.anchoredPosition = Vector2.zero;
        frameView.Apply(PuzzleFrameStyle.Default, previewContent);
        SetPreviewSpawnArea(leftArea, true);
        SetPreviewSpawnArea(rightArea, false);
    }

    private static Vector2 FitPreviewContent(Texture2D texture)
    {
        const float maxWidth = 850f;
        const float maxHeight = 680f;
        var aspect = texture != null ? texture.width / (float)Mathf.Max(1, texture.height) : 4f / 3f;
        var width = maxWidth;
        var height = width / Mathf.Max(0.01f, aspect);
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    private static void SetPreviewSpawnArea(RectTransform rect, bool left)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = rect.anchorMax = new Vector2(left ? 0f : 1f, 0.5f);
        rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(345f, 1024f);
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        child.transform.SetParent(parent, false);
        var text = child.GetComponent<Text>();
        defaultFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = defaultFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Ink;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color color)
    {
        var buttonImage = CreateImage(name, parent, color, true);
        var button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        var text = CreateText("Label", button.transform, label, 21, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 0f);
        text.color = Color.white;
        return button;
    }

    private static Slider CreateSlider(string name, Transform parent)
    {
        var root = CreateImage(name, parent, new Color(0.34f, 0.28f, 0.20f, 0.35f), false);
        var slider = root.gameObject.AddComponent<Slider>();
        var fill = CreateImage("Fill", root.transform, Gold, false);
        fill.rectTransform.anchorMin = new Vector2(0f, 0.25f);
        fill.rectTransform.anchorMax = new Vector2(1f, 0.75f);
        fill.rectTransform.offsetMin = new Vector2(5f, 0f);
        fill.rectTransform.offsetMax = new Vector2(-5f, 0f);
        var handle = CreateImage("Handle", root.transform, new Color(0.96f, 0.86f, 0.59f, 1f), true);
        handle.rectTransform.sizeDelta = new Vector2(24f, 44f);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 2f;
        slider.maxValue = 8f;
        slider.wholeNumbers = true;
        slider.value = 4f;
        return slider;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * inset;
        rect.offsetMax = Vector2.one * -inset;
    }

    private static void SetCentered(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void SetTop(RectTransform rect, float top, float height, float width = 0f, float horizontalOffset = 0f)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(horizontalOffset, -top);
        rect.sizeDelta = new Vector2(width <= 0f ? 620f : width, height);
    }

    private static void SetBottom(RectTransform rect, float bottom, float height, float width, float horizontalOffset)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(horizontalOffset, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }
}
#endif
