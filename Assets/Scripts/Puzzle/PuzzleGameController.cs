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
    private const float FullPieceScale = 1f;
    private const float GroupHoverScale = 1.04f;
    private const float SpawnPadding = 16f;
    private const float MinimumVisiblePieceFraction = 0.10f;
    public const float DefaultSidePieceScale = 0.9f;

    [Header("Built-in artwork")]
    [SerializeField] private List<PuzzleMediaEntry> mediaEntries = new();
    [Header("Piece presentation")]
    [SerializeField] [Range(0.45f, 1f)] private float sideAreaPieceScale = DefaultSidePieceScale;
    [SerializeField] private PuzzlePieceSizeMode pieceSizeMode = PuzzlePieceSizeMode.Dynamic;

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
    [SerializeField] private PuzzleSettingsView settingsView;
    [SerializeField] private Color backgroundColor = Color.black;
    [SerializeField] private bool previewEnabled = true;
    [SerializeField] private bool lockPlacedPieces = true;

    private readonly List<PuzzlePieceView> activePieces = new();
    private readonly List<PuzzlePieceGroup> activeGroups = new();
    private Texture2D selectedTexture;
    private PuzzleMediaEntry activeMedia;
    private PuzzleMediaEntry pendingMedia;
    private PuzzlePieceShape[,] currentShapes;
    private PuzzlePieceGroup draggingGroup;
    private PuzzlePieceGroup hoveredGroup;
    private PuzzlePieceView draggedPiece;
    private Vector2 dragGrabPoint;
    private PuzzleLayoutMetrics currentLayout;
    private int activeColumns = 4;
    private int activeRows = 4;
    private bool puzzleCompleted;
    private bool uiWired;
    private bool settingsOpen;
    private bool waitingForResetConfirmation;
    private PuzzlePieceMode activePieceMode;
    private PuzzlePieceMode pendingPieceMode;
    private Vector2 lastCanvasSize;
    private float currentSidePieceScale = DefaultSidePieceScale;
    private int previousFrameRate;

    public bool PreviewEnabled => previewEnabled;
    public bool LockPlacedPieces => lockPlacedPieces;
    public PuzzlePieceSizeMode PieceSizeMode => pieceSizeMode;

    private void Awake()
    {
        previousFrameRate = Application.targetFrameRate;
        Application.targetFrameRate = 60;
        EnsureEventSystem();
        EnsureMediaEntries();
        WireUi();
        activeMedia ??= mediaEntries.Count > 0 ? mediaEntries[0].Copy() : null;
        if (settingsView != null)
        {
            settingsView.Canvas.gameObject.SetActive(false);
            settingsView.ShowConfirmation(false);
        }
        themeView?.Apply(backgroundColor);
    }

    private void Start() => StartConfiguredPuzzle();

    private void Update()
    {
        RefreshResponsiveLayout();
        if (!WasEscapePressed()) return;
        if (settingsOpen && settingsView != null && settingsView.CloseTransient()) return;
        if (waitingForResetConfirmation) { HideResetConfirmation(); return; }
        if (settingsOpen) CloseSettings(false); else OpenSettings();
    }

    private void OnDestroy()
    {
        Application.targetFrameRate = previousFrameRate;
        ClearExistingPuzzle();
        completionVideo?.StopPlayback();
        DestroyTexture(ref selectedTexture);
        DestroyRuntimeEntryImage(activeMedia);
        DestroyRuntimeEntryImage(pendingMedia);
    }

    public void ConfigureScene(
        Canvas gameplayCanvas, RectTransform gameplayRoot, RectTransform dynamicPiecesLayer,
        RectTransform guideRect, RawImage guideImage, RectTransform leftArea, RectTransform rightArea,
        PuzzleBoardLayout layout, PuzzleFrameView frame, PuzzleCompletionVideo video,
        PuzzleThemeView theme, PuzzleSettingsView settings, List<PuzzleMediaEntry> configuredMedia)
    {
        gameCanvas = gameplayCanvas; gameCanvasRoot = gameplayRoot; piecesLayer = dynamicPiecesLayer;
        boardGuideRect = guideRect; boardGuideImage = guideImage; leftPiecesArea = leftArea; rightPiecesArea = rightArea;
        boardLayout = layout; frameView = frame; completionVideo = video; themeView = theme; settingsView = settings;
        mediaEntries = configuredMedia ?? new List<PuzzleMediaEntry>(); uiWired = false;
    }

    public List<PuzzleMediaEntry> CopyConfiguredMedia() => mediaEntries.ConvertAll(entry => entry.Copy());

    public void ConfigurePiecePresentation(float scale) => sideAreaPieceScale = Mathf.Clamp(scale, 0.45f, 1f);

    private void EnsureMediaEntries()
    {
        if (mediaEntries.Count > 0) return;
        var defaultMedia = PuzzleMediaEntry.LoadDefault();
        if (defaultMedia != null) mediaEntries.Add(defaultMedia);
    }

    private void WireUi()
    {
        if (uiWired || settingsView == null) return;
        settingsView.Bind(LoadCustomImage, LoadCustomVideo, ApplySettings, () => CloseSettings(false),
            ConfirmReset, HideResetConfirmation, SelectPieceMode, SetBackground, SetPreview, SetPlacementLock, SetPieceSizeMode, QuitApplication);
        uiWired = true;
    }

    private void StartConfiguredPuzzle()
    {
        if (!HasGameBindings() || activeMedia?.Image == null) { ShowSettingsStatus("Добавьте изображение для пазла в Inspector или через меню Esc.", true); OpenSettings(); return; }
        ClearExistingPuzzle(); completionVideo?.StopPlayback(); completionVideo?.ResetPresentation(); DestroyTexture(ref selectedTexture);
        selectedTexture = PreparePuzzleTexture(activeMedia.Image, $"Puzzle_{activeMedia.DisplayName}"); puzzleCompleted = false;
        if (gameCanvas != null) gameCanvas.gameObject.SetActive(true);
        var contentSize = boardLayout.Apply(selectedTexture, activeMedia.FrameStyle); Canvas.ForceUpdateCanvases();
        if (contentSize.x <= 0f || contentSize.y <= 0f) { ShowSettingsStatus("Не удалось рассчитать размер игрового поля.", true); return; }
        UpdateBoardGuide();
        currentLayout = CalculateLayoutMetrics(activeRows, activeColumns);
        UpdateSidePieceScale();
        currentShapes = PuzzlePieceGenerator.GenerateShapes(activeRows, activeColumns, activePieceMode);
        lastCanvasSize = gameCanvasRoot.rect.size;
        FitSettingsPanel();
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
            pieceRect.anchoredPosition = startPositions[startIndex++]; pieceRect.localScale = Vector3.one * currentSidePieceScale;
            var image = pieceObject.GetComponent<Image>(); image.sprite = sprite; image.raycastTarget = true; image.preserveAspect = true; image.useSpriteMesh = true; image.alphaHitTestMinimumThreshold = 0.05f;
            var outline = pieceObject.GetComponent<Outline>(); outline.effectColor = new Color(0f, 0f, 0f, 0.3f); outline.effectDistance = new Vector2(1.2f, -1.2f);
            var view = pieceObject.GetComponent<PuzzlePieceView>(); view.Initialize(this, sprite, row, column, GetPieceCorrectPosition(slice));
            var group = new PuzzlePieceGroup(view); activePieces.Add(view); activeGroups.Add(group);
        }
        // The guide only samples on the GPU after generation. Keep piece
        // textures readable for transparent pointer hit testing.
        selectedTexture.Apply(false, true);
    }

    public void BeginDrag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (settingsOpen || puzzleCompleted || piece?.Group == null || piecesLayer == null || (lockPlacedPieces && piece.Group.IsPlaced)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var point)) return;
        var grabbedPoint = piece.RectTransform.InverseTransformPoint(piecesLayer.TransformPoint(point));
        ClearHover();
        draggingGroup = piece.Group;
        draggedPiece = piece;
        dragGrabPoint = grabbedPoint;
        draggingGroup.SetPlaced(false);
        SetGroupScale(draggingGroup, FullPieceScale);
        draggingGroup.MoveBy(point - ConvertLocalPoint(piece.RectTransform, piecesLayer, grabbedPoint));
        draggingGroup.SetHighlighted(true);
        draggingGroup.BringToFront();
    }

    public void Drag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (draggingGroup == null || draggedPiece == null || piece == null || piece.Group != draggingGroup) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var point)) return;
        draggingGroup.MoveBy(point - ConvertLocalPoint(draggedPiece.RectTransform, piecesLayer, dragGrabPoint));
    }

    public void EndDrag(PuzzlePieceView piece)
    {
        if (draggingGroup == null || piece == null || piece.Group != draggingGroup) { draggingGroup = null; draggedPiece = null; return; }
        var group = draggingGroup;
        draggingGroup = null;
        draggedPiece = null;
        KeepGroupOnScreen(group);
        var resolved = ResolvePlacement(group);
        ApplyGroupRestingScale(resolved);
        resolved.SetHighlighted(false);
        if (!resolved.IsPlaced) KeepGroupOnScreen(resolved);
        EvaluateCompletion();
    }

    public void BeginHover(PuzzlePieceView piece)
    {
        if (settingsOpen || puzzleCompleted || piece?.Group == null || draggingGroup != null || (lockPlacedPieces && piece.Group.IsPlaced)) return;
        if (hoveredGroup == piece.Group) return;
        ClearHover();
        hoveredGroup = piece.Group;
        SetGroupScale(hoveredGroup, hoveredGroup.Members.Count == 1 && !hoveredGroup.IsPlaced ? FullPieceScale : GroupHoverScale);
        piece.Group.SetHighlighted(true);
        piece.Group.BringToFront();
    }

    public void EndHover(PuzzlePieceView piece)
    {
        if (piece?.Group == null || piece.Group != hoveredGroup || draggingGroup != null) return;
        ClearHover();
    }

    private void ClearHover()
    {
        if (hoveredGroup == null) return;
        ApplyGroupRestingScale(hoveredGroup);
        hoveredGroup.SetHighlighted(false);
        hoveredGroup = null;
    }

    private PuzzlePieceGroup ResolvePlacement(PuzzlePieceGroup group)
    {
        var current = group;
        for (var i = 0; i < activePieces.Count; i++)
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
        var changed = !group.IsPlaced || bestDelta.sqrMagnitude > 0.000001f;
        group.MoveBy(bestDelta);
        group.SetPlaced(true);
        SetGroupScale(group, FullPieceScale);
        return changed;
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
        SetGroupScale(movingGroup, FullPieceScale);
        SetGroupScale(bestTarget, FullPieceScale);
        if (movingGroup.IsPlaced && !bestTarget.IsPlaced)
        {
            bestTarget.MoveBy(-bestCorrection);
            movingGroup.Absorb(bestTarget);
            activeGroups.Remove(bestTarget);
            mergedGroup = movingGroup;
        }
        else
        {
            movingGroup.MoveBy(bestCorrection);
            bestTarget.Absorb(movingGroup);
            activeGroups.Remove(movingGroup);
            mergedGroup = bestTarget;
        }
        mergedGroup.BringToFront();
        return true;
    }

    private void EvaluateCompletion()
    {
        if (!puzzleCompleted && activePieces.Count > 0 && activeMedia != null)
        {
            foreach (var group in activeGroups)
                if (group.Members.Count * 4 >= activePieces.Count * 3)
                {
                    completionVideo?.Prepare(activeMedia);
                    break;
                }
        }
        if (activePieces.Count == 0 || activeGroups.Count != 1) return;
        foreach (var piece in activePieces)
            if ((piece.RectTransform.anchoredPosition - piece.CorrectAnchoredPosition).sqrMagnitude > 0.25f) return;
        puzzleCompleted = true; SetGroupScale(activeGroups[0], FullPieceScale);
        boardGuideImage.color = Color.white;
        foreach (var piece in activePieces) piece.ReleaseArtwork();
        completionVideo?.Play(activeMedia);
    }

    private void OpenSettings()
    {
        if (settingsView == null) return;
        ClearHover();
        EnsureMediaEntries();
        settingsOpen = true;
        pendingMedia = activeMedia != null ? activeMedia.Copy() : mediaEntries[0].Copy();
        pendingPieceMode = activePieceMode;
        if (draggingGroup != null)
        {
            ApplyGroupRestingScale(draggingGroup);
            draggingGroup.SetHighlighted(false);
            KeepGroupOnScreen(draggingGroup);
        }
        foreach (var group in activeGroups) group.SetHighlighted(false);
        draggingGroup = null;
        draggedPiece = null;
        completionVideo?.SetPaused(true);
        settingsView.Canvas.gameObject.SetActive(true);
        settingsView.SetPending(activeColumns, activeRows, pendingPieceMode, backgroundColor);
        settingsView.SetGameplayPreferences(previewEnabled, lockPlacedPieces, pieceSizeMode);
        settingsView.Fit();
        RefreshSettingsLabels();
    }

    private void CloseSettings(bool keepPending)
    {
        if (!keepPending) { DestroyPendingRuntimeImageIfUnused(); pendingMedia = null; }
        settingsOpen = false; waitingForResetConfirmation = false;
        if (settingsView != null)
        {
            settingsView.CloseTransient();
            settingsView.ShowConfirmation(false);
            settingsView.Canvas.gameObject.SetActive(false);
        }
        completionVideo?.SetPaused(false);
    }

    private void LoadCustomImage()
    {
        var path = NativeImagePicker.OpenImageFile();
        if (string.IsNullOrWhiteSpace(path)) { ShowSettingsStatus("Выбор фотографии отменён."); return; }
        Texture2D texture = null;
        try
        {
            var bytes = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(path), filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            if (!texture.LoadImage(bytes, true)) { Destroy(texture); ShowSettingsStatus("Не удалось открыть выбранное изображение.", true); return; }
            DestroyPendingRuntimeImageIfUnused(); pendingMedia = new PuzzleMediaEntry(texture.name, texture) { IsRuntimeImage = true }; RefreshSettingsLabels();
            ShowSettingsStatus("Фотография выбрана. Выберите видео для её оживления или примените пазл без видео.");
        }
        catch (Exception exception)
        {
            if (texture != null) Destroy(texture);
            Debug.LogWarning($"TileVerse: image loading failed: {exception.Message}");
            ShowSettingsStatus("Не удалось загрузить фото. Проверьте файл и выберите его снова.", true);
        }
    }

    private void LoadCustomVideo()
    {
        if (pendingMedia == null) { ShowSettingsStatus("Сначала выберите фотографию.", true); return; }
        var path = NativeVideoPicker.OpenVideoFile();
        if (string.IsNullOrWhiteSpace(path)) { ShowSettingsStatus("Выбор видео отменён."); return; }
        pendingMedia.SetCustomVideoPath(path); RefreshSettingsLabels(); ShowSettingsStatus("Фото и видео готовы. Нажмите «Применить».");
    }

    private void SetBackground(Color color)
    {
        color.a = 1f;
        backgroundColor = color;
        themeView?.Apply(color);
        settingsView?.SetBackground(color);
    }

    private void SetPreview(bool enabled)
    {
        previewEnabled = enabled;
        RefreshGuideVisibility();
        settingsView?.SetGameplayPreferences(previewEnabled, lockPlacedPieces, pieceSizeMode);
    }

    private void SetPlacementLock(bool enabled)
    {
        ClearHover();
        lockPlacedPieces = enabled;
        foreach (var group in activeGroups) group.SetHighlighted(false);
        settingsView?.SetGameplayPreferences(previewEnabled, lockPlacedPieces, pieceSizeMode);
    }

    private void SetPieceSizeMode(PuzzlePieceSizeMode mode)
    {
        if ((uint)mode > (uint)PuzzlePieceSizeMode.Original) return;
        ClearHover();
        pieceSizeMode = mode;
        UpdateSidePieceScale();
        foreach (var group in activeGroups)
        {
            if (group == draggingGroup) continue;
            ApplyGroupRestingScale(group);
            if (!group.IsPlaced) KeepGroupOnScreen(group);
        }
        settingsView?.SetPieceSizeMode(mode);
    }

    private void QuitApplication()
    {
        // Keep the last video frame visible until Unity closes the window.
        // OnDestroy owns playback and render-target cleanup.
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshGuideVisibility()
    {
        if (boardGuideImage != null)
            boardGuideImage.color = new Color(1f, 1f, 1f, puzzleCompleted ? 1f : previewEnabled ? 0.13f : 0f);
    }

    private void SelectPieceMode(PuzzlePieceMode mode)
    {
        pendingPieceMode = mode;
        settingsView?.SetMode(mode);
    }

    private void ApplySettings()
    {
        if (pendingMedia?.Image == null) { ShowSettingsStatus("Выберите фотографию для пазла.", true); return; }
        var columns = settingsView != null ? settingsView.Columns : activeColumns;
        var rows = settingsView != null ? settingsView.Rows : activeRows;
        if (SameMedia(activeMedia, pendingMedia) && columns == activeColumns && rows == activeRows && pendingPieceMode == activePieceMode) { CloseSettings(false); return; }
        if (activePieces.Count > 0 && !puzzleCompleted) { waitingForResetConfirmation = true; settingsView.ShowConfirmation(true); return; }
        CommitPendingSettings();
    }

    private void ConfirmReset() => CommitPendingSettings();

    private void CommitPendingSettings()
    {
        if (pendingMedia == null) return;
        if (!SameImageReference(activeMedia, pendingMedia)) DestroyRuntimeEntryImage(activeMedia);
        activeMedia = pendingMedia; pendingMedia = null;
        activePieceMode = pendingPieceMode;
        activeColumns = settingsView != null ? settingsView.Columns : activeColumns;
        activeRows = settingsView != null ? settingsView.Rows : activeRows;
        CloseSettings(true); StartConfiguredPuzzle();
    }

    private void HideResetConfirmation()
    {
        waitingForResetConfirmation = false;
        settingsView?.ShowConfirmation(false);
    }

    private void RefreshSettingsLabels() => settingsView?.RefreshMedia(pendingMedia);
    private void FitSettingsPanel() => settingsView?.Fit();

    private void UpdateSidePieceScale()
    {
        if (pieceSizeMode == PuzzlePieceSizeMode.Original)
        {
            currentSidePieceScale = FullPieceScale;
            return;
        }
        var availableWidth = Mathf.Min(leftPiecesArea.rect.width, rightPiecesArea.rect.width) - SpawnPadding * 2f;
        var availableHeight = Mathf.Min(leftPiecesArea.rect.height, rightPiecesArea.rect.height) - SpawnPadding * 2f;
        currentSidePieceScale = Mathf.Max(0.01f, Mathf.Min(sideAreaPieceScale,
            availableWidth / Mathf.Max(1f, currentLayout.MaxPieceUiSize.x),
            availableHeight / Mathf.Max(1f, currentLayout.MaxPieceUiSize.y)));
    }

    private void RefreshResponsiveLayout()
    {
        if (gameCanvasRoot == null) return;
        var size = gameCanvasRoot.rect.size;
        if ((size - lastCanvasSize).sqrMagnitude < 0.5f) return;
        ClearHover();
        FitSettingsPanel();
        if (selectedTexture == null || activePieces.Count == 0) { lastCanvasSize = size; return; }

        var previousSize = lastCanvasSize;
        draggingGroup = null;
        draggedPiece = null;
        boardLayout.Apply(selectedTexture, activeMedia.FrameStyle);
        Canvas.ForceUpdateCanvases();
        currentLayout = CalculateLayoutMetrics(activeRows, activeColumns);
        UpdateSidePieceScale();
        var ratio = new Vector2(size.x / Mathf.Max(1f, previousSize.x), size.y / Mathf.Max(1f, previousSize.y));
        foreach (var group in activeGroups)
        {
            var first = group.Members[0];
            var wasSnapped = (first.RectTransform.anchoredPosition - first.CorrectAnchoredPosition).sqrMagnitude <= 0.25f;
            var anchorPosition = Vector2.Scale(first.RectTransform.anchoredPosition, ratio);
            var anchorSlice = PuzzlePieceGenerator.CalculateSliceBounds(first.Row, first.Column, activeRows, activeColumns, selectedTexture.width, selectedTexture.height);
            var offset = wasSnapped ? Vector2.zero : anchorPosition - GetPieceCorrectPosition(anchorSlice);
            foreach (var piece in group.Members)
            {
                var slice = PuzzlePieceGenerator.CalculateSliceBounds(piece.Row, piece.Column, activeRows, activeColumns, selectedTexture.width, selectedTexture.height);
                piece.SetCorrectPosition(GetPieceCorrectPosition(slice));
                piece.RectTransform.sizeDelta = piece.SourcePixelSize * currentLayout.UiScale;
                piece.RectTransform.anchoredPosition = piece.CorrectAnchoredPosition + offset;
            }
            group.SetPlaced(wasSnapped);
            ApplyGroupRestingScale(group);
            KeepGroupOnScreen(group);
        }
        lastCanvasSize = size;
    }

    private void KeepGroupOnScreen(PuzzlePieceGroup group)
    {
        if (group == null || group.Members.Count == 0) return;
        var bounds = piecesLayer.rect;
        if (GetGroupVisibleFraction(group, bounds, Vector2.zero) >= MinimumVisiblePieceFraction - 0.000001f) return;

        var min = Vector2.one * float.PositiveInfinity;
        var max = Vector2.one * float.NegativeInfinity;
        foreach (var piece in group.Members)
        {
            var artwork = piece.GetArtworkRect();
            min = Vector2.Min(min, artwork.min);
            max = Vector2.Max(max, artwork.max);
        }
        // Run only on release/cancel/resize, never while the pointer is dragging.
        // Find the nearest point along the return path that exposes a useful
        // part of the fragment. Empty space inside a sparse group does not count.
        var towardCenter = bounds.center - (min + max) * 0.5f;
        var target = Mathf.Min(0.20f, GetGroupVisibleFraction(group, bounds, towardCenter));
        var low = 0f;
        var high = 1f;
        for (var i = 0; i < 14; i++)
        {
            var middle = (low + high) * 0.5f;
            var delta = towardCenter * middle;
            if (GetGroupVisibleFraction(group, bounds, delta) >= target && HasReachablePiece(group, bounds, delta)) high = middle;
            else low = middle;
        }
        group.MoveBy(towardCenter * high);
    }

    private static float GetGroupVisibleFraction(PuzzlePieceGroup group, Rect bounds, Vector2 offset)
    {
        var total = 0f;
        var visible = 0f;
        foreach (var piece in group.Members)
        {
            var rect = piece.GetArtworkRect();
            rect.position += offset;
            total += rect.width * rect.height;
            visible += Mathf.Max(0f, Mathf.Min(rect.xMax, bounds.xMax) - Mathf.Max(rect.xMin, bounds.xMin)) *
                Mathf.Max(0f, Mathf.Min(rect.yMax, bounds.yMax) - Mathf.Max(rect.yMin, bounds.yMin));
        }
        return total > 0f ? visible / total : 1f;
    }

    private bool HasReachablePiece(PuzzlePieceGroup group, Rect bounds, Vector2 offset)
    {
        var grip = 32f / Mathf.Max(0.01f, gameCanvas.scaleFactor);
        foreach (var piece in group.Members)
        {
            var rect = piece.GetArtworkRect();
            rect.position += offset;
            var width = Mathf.Min(rect.xMax, bounds.xMax) - Mathf.Max(rect.xMin, bounds.xMin);
            var height = Mathf.Min(rect.yMax, bounds.yMax) - Mathf.Max(rect.yMin, bounds.yMin);
            if (width >= Mathf.Min(grip, Mathf.Min(rect.width, bounds.width)) - 0.01f &&
                height >= Mathf.Min(grip, Mathf.Min(rect.height, bounds.height)) - 0.01f) return true;
        }
        return false;
    }

    private void UpdateBoardGuide()
    {
        boardGuideRect.anchorMin = Vector2.zero; boardGuideRect.anchorMax = Vector2.one; boardGuideRect.offsetMin = Vector2.zero; boardGuideRect.offsetMax = Vector2.zero; boardGuideRect.SetAsFirstSibling();
        boardGuideImage.texture = selectedTexture;
        RefreshGuideVisibility();
        boardGuideImage.raycastTarget = false;
    }

    private PuzzleLayoutMetrics CalculateLayoutMetrics(int rows, int columns)
    {
        var size = boardGuideRect.rect.size; var maxCellWidth = Mathf.Ceil(selectedTexture.width / (float)columns); var maxCellHeight = Mathf.Ceil(selectedTexture.height / (float)rows);
        var minCell = Mathf.Min(selectedTexture.width / (float)columns, selectedTexture.height / (float)rows);
        var tab = activePieceMode == PuzzlePieceMode.Tiles ? 0 : Mathf.CeilToInt(minCell * 0.38f + 4f);
        var maxPiece = new Vector2(maxCellWidth + tab * 2f, maxCellHeight + tab * 2f); var scale = Mathf.Max(0.0001f, Mathf.Min(size.x / selectedTexture.width, size.y / selectedTexture.height));
        var cellUi = minCell * scale;
        var pixelsPerUnit = Mathf.Max(0.01f, gameCanvas.scaleFactor);
        var boardSnap = Mathf.Clamp(cellUi * 0.12f, 10f / pixelsPerUnit, 22f / pixelsPerUnit);
        var neighborSnap = Mathf.Clamp(cellUi * 0.10f, 8f / pixelsPerUnit, 20f / pixelsPerUnit);
        return new PuzzleLayoutMetrics(scale, new Vector2(selectedTexture.width * scale, selectedTexture.height * scale), maxPiece * scale, Mathf.Max(12f, minCell * 0.16f * scale), tab, boardSnap, neighborSnap, Vector2.zero);
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
        var piece = currentLayout.MaxPieceUiSize * currentSidePieceScale;
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

    private void ApplyGroupRestingScale(PuzzlePieceGroup group) =>
        SetGroupScale(group, group.Members.Count == 1 && !group.IsPlaced && IsGroupInSideArea(group) ? currentSidePieceScale : FullPieceScale);

    private void SetGroupScale(PuzzlePieceGroup group, float scale)
    {
        if (group == null) return;
        if (pieceSizeMode == PuzzlePieceSizeMode.Original) scale = FullPieceScale;
        if (group.Members.Count > 1 || group.IsPlaced) scale = Mathf.Max(FullPieceScale, scale);
        scale = Mathf.Clamp(scale, 0.01f, GroupHoverScale);
        if (group.Members.Count == 0) return;
        var previousScale = group.Members[0].RectTransform.localScale.x;
        if (Mathf.Approximately(previousScale, scale)) return;
        var center = Vector2.zero;
        foreach (var member in group.Members) center += member.RectTransform.anchoredPosition;
        center /= group.Members.Count;
        var ratio = scale / Mathf.Max(0.01f, previousScale);
        foreach (var member in group.Members)
        {
            // Scale spacing and artwork around one pivot so connected seams stay closed.
            member.RectTransform.anchoredPosition = center + (member.RectTransform.anchoredPosition - center) * ratio;
            member.RectTransform.localScale = Vector3.one * scale;
        }
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
        hoveredGroup = null;
        draggedPiece = null;
        foreach (var piece in activePieces)
        {
            if (piece == null) continue;
            piece.ReleaseArtwork();
            Destroy(piece.gameObject);
        }
        activePieces.Clear(); activeGroups.Clear(); draggingGroup = null;
    }

    private void ShowSettingsStatus(string message, bool isError = false)
    {
        settingsView?.SetStatus(isError ? message : string.Empty, isError);
    }

    private bool HasGameBindings() => gameCanvas != null && gameCanvasRoot != null && piecesLayer != null && boardGuideRect != null && boardGuideImage != null && leftPiecesArea != null && rightPiecesArea != null && boardLayout != null && frameView != null;
    private static bool AreNeighbors(PuzzlePieceView first, PuzzlePieceView second) => Mathf.Abs(first.Row - second.Row) + Mathf.Abs(first.Column - second.Column) == 1;
    private static Vector2 ConvertLocalPoint(RectTransform from, RectTransform to, Vector2 point) => to.InverseTransformPoint(from.TransformPoint(point));



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

}
