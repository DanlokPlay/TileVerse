using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PuzzleGameController : MonoBehaviour
{
    private const int MinGridSize = 2;
    private const int MaxGridSize = 8;
    private const float FullPieceScale = 1f;
    private const float SpawnPadding = 16f;

    [Header("Built-in artwork")]
    [SerializeField] private Texture2D mountainsTexture;
    [SerializeField] private Texture2D gladeTexture;
    [SerializeField] private List<PuzzleMediaEntry> mediaEntries = new();
    [Header("Piece presentation")]
    [SerializeField] [Range(0.45f, 1f)] private float sideAreaPieceScale = 0.72f;

    [Header("Gameplay scene objects")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private RectTransform gameCanvasRoot;
    [SerializeField] private RectTransform piecesLayer;
    [SerializeField] private RectTransform boardGuideRect;
    [SerializeField] private RawImage boardGuideImage;
    [SerializeField] private RectTransform leftPiecesArea;
    [SerializeField] private RectTransform rightPiecesArea;
    [SerializeField] private PuzzleBoardLayout boardLayout;
    [SerializeField] private PuzzleFrameView frameView;
    [SerializeField] private PuzzleCompletionVideo completionVideo;
    [SerializeField] private PuzzleThemeView themeView;

    [Header("Settings overlay")]
    [SerializeField] private Canvas settingsCanvas;
    [SerializeField] private Text selectedImageText;
    [SerializeField] private Text settingsStatusText;
    [SerializeField] private Text horizontalValueText;
    [SerializeField] private Text verticalValueText;
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private Button builtInButtonA;
    [SerializeField] private Button builtInButtonB;
    [SerializeField] private Button customImageButton;
    [SerializeField] private Button customVideoButton;
    [SerializeField] private Button themeButton;
    [SerializeField] private Button applySettingsButton;
    [SerializeField] private Button closeSettingsButton;
    [SerializeField] private GameObject resetConfirmationDialog;
    [SerializeField] private Button resetConfirmButton;
    [SerializeField] private Button resetCancelButton;

    private readonly List<PuzzlePieceView> activePieces = new();
    private readonly List<PuzzlePieceGroup> activeGroups = new();
    private Texture2D selectedTexture;
    private PuzzleMediaEntry activeMedia;
    private PuzzleMediaEntry pendingMedia;
    private PuzzlePieceShape[,] currentShapes;
    private PuzzlePieceGroup draggingGroup;
    private Vector2 lastPointerLocalPoint;
    private PuzzleLayoutMetrics currentLayout;
    private int activeColumns = 4;
    private int activeRows = 4;
    private bool puzzleCompleted;
    private bool uiWired;
    private bool settingsOpen;
    private bool waitingForResetConfirmation;
    private bool darkTheme;

    private void Awake()
    {
        EnsureEventSystem();
        EnsureMediaEntries();
        WireUi();
        RefreshGridLabels();
        activeMedia ??= mediaEntries.Count > 0 ? mediaEntries[0].Copy() : null;
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false);
        if (resetConfirmationDialog != null) resetConfirmationDialog.SetActive(false);
        themeView?.Apply(darkTheme);
    }

    private void Start() => StartConfiguredPuzzle();

    private void Update()
    {
        if (!WasEscapePressed()) return;
        if (waitingForResetConfirmation) { HideResetConfirmation(); return; }
        if (settingsOpen) CloseSettings(false); else OpenSettings();
    }

    private void OnDestroy()
    {
        ClearExistingPuzzle();
        completionVideo?.StopPlayback();
        DestroyTexture(ref selectedTexture);
        DestroyRuntimeEntryImage(activeMedia);
        DestroyRuntimeEntryImage(pendingMedia);
    }

    public void ConfigureScene(
        Canvas gameplayCanvas, RectTransform gameplayRoot, RectTransform dynamicPiecesLayer, RectTransform guideRect, RawImage guideImage,
        RectTransform leftArea, RectTransform rightArea, PuzzleBoardLayout layout, PuzzleFrameView frame, PuzzleCompletionVideo video, PuzzleThemeView theme,
        Canvas menuCanvas, Text selectedLabel, Text statusLabel, Text horizontalLabel, Text verticalLabel, Slider horizontal, Slider vertical,
        Button builtInA, Button builtInB, Button customImage, Button customVideo, Button themeToggle, Button applyButton, Button closeButton,
        GameObject confirmation, Button confirmationAccept, Button confirmationCancel, List<PuzzleMediaEntry> configuredMedia)
    {
        gameCanvas = gameplayCanvas; gameCanvasRoot = gameplayRoot; piecesLayer = dynamicPiecesLayer; boardGuideRect = guideRect; boardGuideImage = guideImage;
        leftPiecesArea = leftArea; rightPiecesArea = rightArea; boardLayout = layout; frameView = frame; completionVideo = video; themeView = theme;
        settingsCanvas = menuCanvas; selectedImageText = selectedLabel; settingsStatusText = statusLabel; horizontalValueText = horizontalLabel;
        verticalValueText = verticalLabel; horizontalSlider = horizontal; verticalSlider = vertical; builtInButtonA = builtInA; builtInButtonB = builtInB;
        customImageButton = customImage; customVideoButton = customVideo; themeButton = themeToggle; applySettingsButton = applyButton;
        closeSettingsButton = closeButton; resetConfirmationDialog = confirmation; resetConfirmButton = confirmationAccept; resetCancelButton = confirmationCancel;
        mediaEntries = configuredMedia ?? new List<PuzzleMediaEntry>(); uiWired = false;
    }

    private void EnsureMediaEntries()
    {
        if (mediaEntries.Count > 0) return;
        if (mountainsTexture != null) mediaEntries.Add(new PuzzleMediaEntry("Горы", mountainsTexture));
        if (gladeTexture != null) mediaEntries.Add(new PuzzleMediaEntry("Поляна", gladeTexture));
        if (mediaEntries.Count > 0) return;
        mediaEntries.Add(new PuzzleMediaEntry("Закатная долина", PuzzleTextureFactory.CreateSunsetTexture(1200, 1200)) { IsRuntimeImage = true });
        mediaEntries.Add(new PuzzleMediaEntry("Геометрический ритм", PuzzleTextureFactory.CreateGeometryTexture(1200, 1200)) { IsRuntimeImage = true });
    }

    private void WireUi()
    {
        if (uiWired) return;
        ConfigureGridSlider(horizontalSlider, activeColumns); ConfigureGridSlider(verticalSlider, activeRows);
        if (horizontalSlider != null) horizontalSlider.onValueChanged.AddListener(_ => RefreshGridLabels());
        if (verticalSlider != null) verticalSlider.onValueChanged.AddListener(_ => RefreshGridLabels());
        builtInButtonA?.onClick.AddListener(() => SelectBuiltInEntry(0)); builtInButtonB?.onClick.AddListener(() => SelectBuiltInEntry(1));
        customImageButton?.onClick.AddListener(LoadCustomImage); customVideoButton?.onClick.AddListener(LoadCustomVideo);
        themeButton?.onClick.AddListener(ToggleTheme); applySettingsButton?.onClick.AddListener(ApplySettings);
        closeSettingsButton?.onClick.AddListener(() => CloseSettings(false)); resetConfirmButton?.onClick.AddListener(ConfirmReset); resetCancelButton?.onClick.AddListener(HideResetConfirmation);
        SetButtonLabel(builtInButtonA, mediaEntries.Count > 0 ? mediaEntries[0].DisplayName : "Картина 1");
        SetButtonLabel(builtInButtonB, mediaEntries.Count > 1 ? mediaEntries[1].DisplayName : "Картина 2");
        SetButtonLabel(themeButton, "Тема: светлая"); uiWired = true;
    }

    private static void ConfigureGridSlider(Slider slider, int value)
    {
        if (slider == null) return;
        slider.minValue = MinGridSize; slider.maxValue = MaxGridSize; slider.wholeNumbers = true; slider.value = Mathf.Clamp(value, MinGridSize, MaxGridSize);
    }

    private void StartConfiguredPuzzle()
    {
        if (!HasGameBindings() || activeMedia?.Image == null) { ShowSettingsStatus("Добавьте изображение для пазла в Inspector или через меню Esc.", true); OpenSettings(); return; }
        ClearExistingPuzzle(); completionVideo?.StopPlayback(); completionVideo?.ResetPresentation(); DestroyTexture(ref selectedTexture);
        selectedTexture = PreparePuzzleTexture(activeMedia.Image, $"Puzzle_{activeMedia.DisplayName}"); puzzleCompleted = false;
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);
        var contentSize = boardLayout.Apply(selectedTexture, activeMedia.FrameStyle); Canvas.ForceUpdateCanvases();
        if (contentSize.x <= 0f || contentSize.y <= 0f) { ShowSettingsStatus("Не удалось рассчитать размер игрового поля.", true); return; }
        currentLayout = CalculateLayoutMetrics(activeRows, activeColumns); currentShapes = PuzzlePieceGenerator.GenerateShapes(activeRows, activeColumns); UpdateBoardGuide();
        var startPositions = BuildStartPositions(activeRows * activeColumns); var startIndex = 0;
        for (var row = 0; row < activeRows; row++)
        for (var column = 0; column < activeColumns; column++)
        {
            var slice = PuzzlePieceGenerator.CalculateSliceBounds(row, column, activeRows, activeColumns, selectedTexture.width, selectedTexture.height);
            var sprite = PuzzlePieceGenerator.CreatePieceSprite(selectedTexture, slice, currentShapes[row, column], currentLayout.TabMarginSource);
            var pieceObject = new GameObject($"Piece {row}-{column}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PuzzlePieceView));
            pieceObject.transform.SetParent(piecesLayer, false);
            var pieceRect = (RectTransform)pieceObject.transform;
            pieceRect.sizeDelta = new Vector2(sprite.rect.width * currentLayout.UiScale, sprite.rect.height * currentLayout.UiScale);
            pieceRect.anchoredPosition = startPositions[startIndex++]; pieceRect.localScale = Vector3.one * sideAreaPieceScale;
            var image = pieceObject.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = true; image.preserveAspect = true; image.useSpriteMesh = true; image.alphaHitTestMinimumThreshold = 0.05f;
            var outline = pieceObject.GetComponent<Outline>(); outline.effectColor = new Color(0f, 0f, 0f, 0.3f); outline.effectDistance = new Vector2(1.2f, -1.2f);
            var view = pieceObject.GetComponent<PuzzlePieceView>(); view.Initialize(this, sprite, row, column, GetPieceCorrectPosition(slice));
            var group = new PuzzlePieceGroup(view); activePieces.Add(view); activeGroups.Add(group);
        }
    }

    public void BeginDrag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (puzzleCompleted || piece?.Group == null || piecesLayer == null) return;
        draggingGroup = piece.Group; SetGroupScale(draggingGroup, FullPieceScale); draggingGroup.BringToFront();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var point)) lastPointerLocalPoint = point;
    }

    public void Drag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (draggingGroup == null || piece == null || piece.Group != draggingGroup) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var point)) return;
        draggingGroup.MoveBy(point - lastPointerLocalPoint); lastPointerLocalPoint = point;
    }

    public void EndDrag(PuzzlePieceView piece)
    {
        if (draggingGroup == null || piece == null || piece.Group != draggingGroup) { draggingGroup = null; return; }
        var group = draggingGroup; draggingGroup = null; var resolved = ResolvePlacement(group); ApplyGroupRestingScale(resolved); EvaluateCompletion();
    }

    public void BeginHover(PuzzlePieceView piece)
    {
        if (puzzleCompleted || piece?.Group == null || piece.Group == draggingGroup) return;
        SetGroupScale(piece.Group, FullPieceScale); piece.Group.BringToFront();
    }

    public void EndHover(PuzzlePieceView piece)
    {
        if (puzzleCompleted || piece?.Group == null || piece.Group == draggingGroup) return;
        ApplyGroupRestingScale(piece.Group);
    }

    private PuzzlePieceGroup ResolvePlacement(PuzzlePieceGroup group)
    {
        var current = group;
        for (var i = 0; i < 12; i++)
        {
            var changed = TrySnapGroupToBoard(current);
            if (TryMergeWithNeighbor(current, out var merged)) { current = merged; changed = true; }
            if (TrySnapGroupToBoard(current)) changed = true;
            if (!changed) break;
        }
        return current;
    }

    private bool TrySnapGroupToBoard(PuzzlePieceGroup group)
    {
        var bestDistance = float.MaxValue; var bestDelta = Vector2.zero;
        foreach (var piece in group.Members)
        {
            var delta = piece.CorrectAnchoredPosition - piece.RectTransform.anchoredPosition;
            if (Mathf.Abs(delta.x) > currentLayout.BoardSnapThreshold || Mathf.Abs(delta.y) > currentLayout.BoardSnapThreshold) continue;
            if (delta.sqrMagnitude < bestDistance) { bestDistance = delta.sqrMagnitude; bestDelta = delta; }
        }
        if (bestDistance == float.MaxValue) return false;
        group.MoveBy(bestDelta); return true;
    }

    private bool TryMergeWithNeighbor(PuzzlePieceGroup movingGroup, out PuzzlePieceGroup mergedGroup)
    {
        var bestDistance = float.MaxValue; var bestCorrection = Vector2.zero; PuzzlePieceGroup bestTarget = null;
        foreach (var target in activeGroups)
        {
            if (target == movingGroup) continue;
            foreach (var moving in movingGroup.Members)
            foreach (var stationary in target.Members)
            {
                if (!AreNeighbors(moving, stationary)) continue;
                var expected = stationary.CorrectAnchoredPosition - moving.CorrectAnchoredPosition;
                var correction = stationary.RectTransform.anchoredPosition - expected - moving.RectTransform.anchoredPosition;
                if (Mathf.Abs(correction.x) > currentLayout.MergeThreshold || Mathf.Abs(correction.y) > currentLayout.MergeThreshold) continue;
                if (correction.sqrMagnitude < bestDistance) { bestDistance = correction.sqrMagnitude; bestCorrection = correction; bestTarget = target; }
            }
        }
        if (bestTarget == null) { mergedGroup = movingGroup; return false; }
        movingGroup.MoveBy(bestCorrection); bestTarget.Absorb(movingGroup); bestTarget.BringToFront(); activeGroups.Remove(movingGroup); mergedGroup = bestTarget; return true;
    }

    private void EvaluateCompletion()
    {
        if (activePieces.Count == 0 || activeGroups.Count != 1) return;
        foreach (var piece in activePieces)
            if ((piece.RectTransform.anchoredPosition - piece.CorrectAnchoredPosition).sqrMagnitude > 0.25f) return;
        puzzleCompleted = true; SetGroupScale(activeGroups[0], FullPieceScale); completionVideo?.Play(activeMedia);
    }

    private void OpenSettings()
    {
        if (settingsCanvas == null) return;
        EnsureMediaEntries(); settingsOpen = true; pendingMedia = activeMedia != null ? activeMedia.Copy() : mediaEntries[0].Copy();
        if (horizontalSlider != null) horizontalSlider.value = activeColumns; if (verticalSlider != null) verticalSlider.value = activeRows;
        RefreshGridLabels(); RefreshSettingsLabels(); settingsCanvas.gameObject.SetActive(true);
        ShowSettingsStatus("Настройте картину и пазл. Изменения фото, видео или сетки потребуют подтверждения.");
    }

    private void CloseSettings(bool keepPending)
    {
        if (!keepPending) { DestroyPendingRuntimeImageIfUnused(); pendingMedia = null; }
        settingsOpen = false; waitingForResetConfirmation = false;
        if (resetConfirmationDialog != null) resetConfirmationDialog.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.gameObject.SetActive(false);
    }

    private void SelectBuiltInEntry(int index)
    {
        if (index < 0 || index >= mediaEntries.Count) return;
        DestroyPendingRuntimeImageIfUnused(); pendingMedia = mediaEntries[index].Copy(); RefreshSettingsLabels();
        ShowSettingsStatus($"Выбрана картина «{pendingMedia.DisplayName}». Видео задаётся в Inspector или кнопкой ниже.");
    }

    private void LoadCustomImage()
    {
        var path = NativeImagePicker.OpenImageFile();
        if (string.IsNullOrWhiteSpace(path)) { ShowSettingsStatus("Выбор фотографии отменён."); return; }
        try
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(path), filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            if (!texture.LoadImage(File.ReadAllBytes(path), false)) { Destroy(texture); ShowSettingsStatus("Не удалось открыть выбранное изображение.", true); return; }
            DestroyPendingRuntimeImageIfUnused(); pendingMedia = new PuzzleMediaEntry(texture.name, texture) { IsRuntimeImage = true }; RefreshSettingsLabels();
            ShowSettingsStatus("Фотография выбрана. Выберите видео для её оживления или примените пазл без видео.");
        }
        catch (Exception exception) { ShowSettingsStatus($"Ошибка загрузки фотографии: {exception.Message}", true); }
    }

    private void LoadCustomVideo()
    {
        if (pendingMedia == null) { ShowSettingsStatus("Сначала выберите фотографию.", true); return; }
        var path = NativeVideoPicker.OpenVideoFile();
        if (string.IsNullOrWhiteSpace(path)) { ShowSettingsStatus("Выбор видео отменён."); return; }
        pendingMedia.SetCustomVideoPath(path); RefreshSettingsLabels(); ShowSettingsStatus("Видео связано с выбранной фотографией для этого запуска.");
    }

    private void ToggleTheme()
    {
        darkTheme = !darkTheme; themeView?.Apply(darkTheme); SetButtonLabel(themeButton, darkTheme ? "Тема: тёмная" : "Тема: светлая");
    }

    private void ApplySettings()
    {
        if (pendingMedia?.Image == null) { ShowSettingsStatus("Выберите фотографию для пазла.", true); return; }
        var columns = Mathf.Clamp(horizontalSlider != null ? Mathf.RoundToInt(horizontalSlider.value) : activeColumns, MinGridSize, MaxGridSize);
        var rows = Mathf.Clamp(verticalSlider != null ? Mathf.RoundToInt(verticalSlider.value) : activeRows, MinGridSize, MaxGridSize);
        if (SameMedia(activeMedia, pendingMedia) && columns == activeColumns && rows == activeRows) { CloseSettings(false); return; }
        if (activePieces.Count > 0 && !puzzleCompleted) { waitingForResetConfirmation = true; if (resetConfirmationDialog != null) resetConfirmationDialog.SetActive(true); return; }
        CommitPendingSettings();
    }

    private void ConfirmReset() => CommitPendingSettings();

    private void CommitPendingSettings()
    {
        if (pendingMedia == null) return;
        if (!SameImageReference(activeMedia, pendingMedia)) DestroyRuntimeEntryImage(activeMedia);
        activeMedia = pendingMedia; pendingMedia = null;
        activeColumns = Mathf.Clamp(horizontalSlider != null ? Mathf.RoundToInt(horizontalSlider.value) : activeColumns, MinGridSize, MaxGridSize);
        activeRows = Mathf.Clamp(verticalSlider != null ? Mathf.RoundToInt(verticalSlider.value) : activeRows, MinGridSize, MaxGridSize);
        CloseSettings(true); StartConfiguredPuzzle();
    }

    private void HideResetConfirmation()
    {
        waitingForResetConfirmation = false; if (resetConfirmationDialog != null) resetConfirmationDialog.SetActive(false);
    }

    private void RefreshSettingsLabels()
    {
        if (selectedImageText != null && pendingMedia != null)
            selectedImageText.text = $"Картина: {pendingMedia.DisplayName}\nВидео: {(pendingMedia.HasVideo ? "выбрано" : "не выбрано")}";
    }

    private void RefreshGridLabels()
    {
        if (horizontalValueText != null && horizontalSlider != null) horizontalValueText.text = Mathf.RoundToInt(horizontalSlider.value).ToString();
        if (verticalValueText != null && verticalSlider != null) verticalValueText.text = Mathf.RoundToInt(verticalSlider.value).ToString();
    }

    private void UpdateBoardGuide()
    {
        boardGuideRect.anchorMin = Vector2.zero; boardGuideRect.anchorMax = Vector2.one; boardGuideRect.offsetMin = Vector2.zero; boardGuideRect.offsetMax = Vector2.zero; boardGuideRect.SetAsFirstSibling();
        boardGuideImage.texture = selectedTexture; boardGuideImage.color = new Color(1f, 1f, 1f, 0.13f); boardGuideImage.raycastTarget = false;
    }

    private PuzzleLayoutMetrics CalculateLayoutMetrics(int rows, int columns)
    {
        var size = boardGuideRect.rect.size; var maxCellWidth = Mathf.Ceil(selectedTexture.width / (float)columns); var maxCellHeight = Mathf.Ceil(selectedTexture.height / (float)rows);
        var minCell = Mathf.Min(selectedTexture.width / (float)columns, selectedTexture.height / (float)rows); var tab = Mathf.CeilToInt(minCell * 0.38f + 4f);
        var maxPiece = new Vector2(maxCellWidth + tab * 2f, maxCellHeight + tab * 2f); var scale = Mathf.Max(0.06f, Mathf.Min(size.x / selectedTexture.width, size.y / selectedTexture.height));
        var cellUi = minCell * scale;
        return new PuzzleLayoutMetrics(scale, new Vector2(selectedTexture.width * scale, selectedTexture.height * scale), maxPiece * scale, Mathf.Max(12f, minCell * 0.16f * scale), tab, Mathf.Max(26f, cellUi * 0.45f), Mathf.Max(22f, cellUi * 0.36f), Vector2.zero);
    }

    private List<Vector2> BuildStartPositions(int count)
    {
        var positions = new List<Vector2>(count); var leftFirst = UnityEngine.Random.value > 0.5f;
        for (var i = 0; i < count; i++) positions.Add(BuildSpawnPosition(((i % 2 == 0) == leftFirst) ? leftPiecesArea : rightPiecesArea, positions));
        for (var i = positions.Count - 1; i > 0; i--) { var swap = UnityEngine.Random.Range(0, i + 1); (positions[i], positions[swap]) = (positions[swap], positions[i]); }
        return positions;
    }

    private Vector2 BuildSpawnPosition(RectTransform area, List<Vector2> existing)
    {
        if (area == null) return Vector2.zero;
        var piece = currentLayout.MaxPieceUiSize * sideAreaPieceScale;
        var half = piece * 0.5f;
        var bounds = area.rect;
        var minX = bounds.xMin + half.x + SpawnPadding;
        var maxX = bounds.xMax - half.x - SpawnPadding;
        var minY = bounds.yMin + half.y + SpawnPadding;
        var maxY = bounds.yMax - half.y - SpawnPadding;
        if (minX > maxX) minX = maxX = bounds.center.x;
        if (minY > maxY) minY = maxY = bounds.center.y;
        var distance = Mathf.Max(18f, Mathf.Min(piece.x, piece.y) * 0.32f);
        for (var attempt = 0; attempt < 160; attempt++)
        {
            var local = new Vector2(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY)); var candidate = ConvertLocalPoint(area, piecesLayer, local);
            var relaxed = Mathf.Lerp(distance, distance * 0.12f, attempt / 159f); var overlaps = false;
            foreach (var position in existing) if ((position - candidate).sqrMagnitude < relaxed * relaxed) { overlaps = true; break; }
            if (!overlaps) return candidate;
        }
        return ConvertLocalPoint(area, piecesLayer, Vector2.zero);
    }

    private Vector2 GetPieceCorrectPosition(PuzzleSliceBounds slice)
    {
        var x = (slice.XMin + slice.XMax) * 0.5f - selectedTexture.width * 0.5f; var y = (slice.YMin + slice.YMax) * 0.5f - selectedTexture.height * 0.5f;
        return ConvertLocalPoint(boardGuideRect, piecesLayer, new Vector2(x * currentLayout.UiScale, y * currentLayout.UiScale));
    }

    private void ApplyGroupRestingScale(PuzzlePieceGroup group) => SetGroupScale(group, IsGroupInSideArea(group) ? sideAreaPieceScale : FullPieceScale);

    private void SetGroupScale(PuzzlePieceGroup group, float scale)
    {
        if (group == null) return;
        foreach (var member in group.Members) if (member != null) member.RectTransform.localScale = Vector3.one * Mathf.Clamp(scale, 0.1f, FullPieceScale);
    }

    private bool IsGroupInSideArea(PuzzlePieceGroup group)
    {
        if (group == null) return false;
        foreach (var member in group.Members)
            if (member != null && (ContainsWorldPoint(leftPiecesArea, member.RectTransform.position) || ContainsWorldPoint(rightPiecesArea, member.RectTransform.position))) return true;
        return false;
    }

    private static bool ContainsWorldPoint(RectTransform rect, Vector3 point) => rect != null && rect.rect.Contains(rect.InverseTransformPoint(point));

    private void ClearExistingPuzzle()
    {
        foreach (var piece in activePieces)
        {
            if (piece == null) continue;
            var sprite = piece.PieceSprite;
            if (sprite != null) { var texture = sprite.texture; Destroy(sprite); if (texture != null) Destroy(texture); }
            Destroy(piece.gameObject);
        }
        activePieces.Clear(); activeGroups.Clear(); draggingGroup = null;
    }

    private void ShowSettingsStatus(string message, bool isError = false)
    {
        if (settingsStatusText == null) return;
        settingsStatusText.text = message; settingsStatusText.color = isError ? new Color(0.72f, 0.12f, 0.10f, 1f) : darkTheme ? new Color(0.92f, 0.88f, 0.75f, 1f) : new Color(0.22f, 0.17f, 0.09f, 1f);
    }

    private bool HasGameBindings() => gameCanvas != null && gameCanvasRoot != null && piecesLayer != null && boardGuideRect != null && boardGuideImage != null && leftPiecesArea != null && rightPiecesArea != null && boardLayout != null && frameView != null;
    private static bool AreNeighbors(PuzzlePieceView first, PuzzlePieceView second) => Mathf.Abs(first.Row - second.Row) + Mathf.Abs(first.Column - second.Column) == 1;
    private static Vector2 ConvertLocalPoint(RectTransform from, RectTransform to, Vector2 point) => to.InverseTransformPoint(from.TransformPoint(point));

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null) return; var text = button.GetComponentInChildren<Text>(true); if (text != null) text.text = label;
    }

    private static bool SameMedia(PuzzleMediaEntry first, PuzzleMediaEntry second) => first == null || second == null ? first == second : first.Image == second.Image && first.VideoClip == second.VideoClip && string.Equals(first.CustomVideoPath, second.CustomVideoPath, StringComparison.Ordinal) && first.LoopVideo == second.LoopVideo;
    private static bool SameImageReference(PuzzleMediaEntry first, PuzzleMediaEntry second) => first != null && second != null && first.Image == second.Image;

    private void DestroyPendingRuntimeImageIfUnused()
    {
        if (pendingMedia != null && !SameImageReference(activeMedia, pendingMedia)) DestroyRuntimeEntryImage(pendingMedia);
    }

    private static void DestroyRuntimeEntryImage(PuzzleMediaEntry entry)
    {
        if (entry == null || !entry.IsRuntimeImage || entry.Image == null) return;
        Destroy(entry.Image); entry.IsRuntimeImage = false;
    }

    private static void DestroyTexture(ref Texture2D texture) { if (texture != null) { Destroy(texture); texture = null; } }

    private static Texture2D PreparePuzzleTexture(Texture source, string name)
    {
        var texture = PuzzleTextureFactory.PrepareReadableTexture(source, 1600, name); texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; return texture;
    }

    private static bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAssignBuiltInTexture(ref mountainsTexture, "Горы"); TryAssignBuiltInTexture(ref gladeTexture, "Поляна");
    }

    private static void TryAssignBuiltInTexture(ref Texture2D texture, string name)
    {
        if (texture != null) return;
        var guids = AssetDatabase.FindAssets($"{name} t:Texture2D", new[] { "Assets" });
        if (guids.Length > 0) texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
#endif
}
