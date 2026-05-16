using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PuzzleGameController : MonoBehaviour
{
    private const int MinGridSize = 2;
    private const int MaxGridSize = 8;
    private const float BoardSidePadding = 32f;
    private const float BoardTopPadding = 112f;
    private const float BoardBottomPadding = 32f;
    private const float SpawnSafetyPadding = 18f;
    private const float FullPieceScale = 1f;

    [Header("Built In Images")]
    [SerializeField] private Texture2D mountainsTexture;
    [SerializeField] private Texture2D gladeTexture;

    [Header("Piece Presentation")]
    [SerializeField] [Range(0.45f, 1f)] private float sourceAreaPieceScale = 0.72f;

    [Header("Menu Canvas")]
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Button menuStartButton;
    [SerializeField] private Button menuExitButton;

    [Header("Setup Canvas")]
    [SerializeField] private Canvas setupCanvas;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Text selectedImageText;
    [SerializeField] private Text setupStatusText;
    [SerializeField] private Text horizontalValueText;
    [SerializeField] private Text verticalValueText;
    [SerializeField] private Slider horizontalSlider;
    [SerializeField] private Slider verticalSlider;
    [SerializeField] private Button builtInButtonA;
    [SerializeField] private Button builtInButtonB;
    [SerializeField] private Button customImageButton;
    [SerializeField] private Button setupStartButton;
    [SerializeField] private Button setupBackButton;

    [Header("Game Canvas")]
    [SerializeField] private Canvas gameCanvas;
    [SerializeField] private RectTransform sourceArea;
    [SerializeField] private RectTransform boardArea;
    [SerializeField] private RectTransform piecesLayer;
    [SerializeField] private RectTransform boardGuideRect;
    [SerializeField] private RawImage boardGuideImage;
    [SerializeField] private Text gameHintText;
    [SerializeField] private Text completionText;
    [SerializeField] private Button gameExitButton;

    private readonly List<PuzzleSourceImage> builtInImages = new();
    private readonly List<PuzzlePieceView> activePieces = new();
    private readonly List<PuzzlePieceGroup> activeGroups = new();

    private Texture2D selectedTexture;
    private Texture2D customTexture;
    private PuzzlePieceShape[,] currentShapes;
    private PuzzlePieceGroup draggingGroup;
    private Vector2 lastPointerLocalPoint;
    private PuzzleLayoutMetrics currentLayout;
    private bool puzzleCompleted;
    private bool uiWired;
    private bool sourceAreaDefaultActive;
    private Vector2 sourceAreaDefaultAnchorMin;
    private Vector2 sourceAreaDefaultAnchorMax;
    private Vector2 sourceAreaDefaultAnchoredPosition;
    private Vector2 sourceAreaDefaultSizeDelta;
    private Vector2 boardAreaDefaultAnchorMin;
    private Vector2 boardAreaDefaultAnchorMax;
    private Vector2 boardAreaDefaultAnchoredPosition;
    private Vector2 boardAreaDefaultSizeDelta;

    private void Awake()
    {
        EnsureEventSystem();
        CacheLayoutState();
        CreateBuiltInImages();
        WireUi();
        RefreshGridLabels();

        if (selectedTexture == null && builtInImages.Count > 0)
        {
            SelectSourceImage(builtInImages[0]);
        }

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }

        if (Application.isPlaying)
        {
            ShowMenuScreen();
        }
    }

    private void CacheLayoutState()
    {
        if (sourceArea != null)
        {
            sourceAreaDefaultActive = sourceArea.gameObject.activeSelf;
            sourceAreaDefaultAnchorMin = sourceArea.anchorMin;
            sourceAreaDefaultAnchorMax = sourceArea.anchorMax;
            sourceAreaDefaultAnchoredPosition = sourceArea.anchoredPosition;
            sourceAreaDefaultSizeDelta = sourceArea.sizeDelta;
        }

        if (boardArea != null)
        {
            boardAreaDefaultAnchorMin = boardArea.anchorMin;
            boardAreaDefaultAnchorMax = boardArea.anchorMax;
            boardAreaDefaultAnchoredPosition = boardArea.anchoredPosition;
            boardAreaDefaultSizeDelta = boardArea.sizeDelta;
        }
    }

    private void OnDestroy()
    {
        ClearExistingPuzzle();
        DestroyTexture(ref customTexture);

        for (var i = 0; i < builtInImages.Count; i++)
        {
            var texture = builtInImages[i].Texture;
            DestroyTexture(ref texture);
            builtInImages[i].Texture = null;
        }
    }

    private void ApplyTextPalette()
    {
        NormalizeCanvasText(menuCanvas);
        NormalizeCanvasText(setupCanvas);
        NormalizeCanvasText(gameCanvas);
    }

    private static void NormalizeCanvasText(Canvas canvas)
    {
        if (canvas == null)
        {
            return;
        }

        var texts = canvas.GetComponentsInChildren<Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            var averageBrightness = (text.color.r + text.color.g + text.color.b) / 3f;
            text.color = averageBrightness >= 0.9f ? Color.white : Color.black;
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private void WireUi()
    {
        if (uiWired)
        {
            return;
        }

        if (horizontalSlider != null)
        {
            horizontalSlider.minValue = MinGridSize;
            horizontalSlider.maxValue = MaxGridSize;
            horizontalSlider.wholeNumbers = true;
            if (horizontalSlider.value < MinGridSize || horizontalSlider.value > MaxGridSize)
            {
                horizontalSlider.value = 4;
            }

            horizontalSlider.onValueChanged.AddListener(_ => RefreshGridLabels());
        }

        if (verticalSlider != null)
        {
            verticalSlider.minValue = MinGridSize;
            verticalSlider.maxValue = MaxGridSize;
            verticalSlider.wholeNumbers = true;
            if (verticalSlider.value < MinGridSize || verticalSlider.value > MaxGridSize)
            {
                verticalSlider.value = 4;
            }

            verticalSlider.onValueChanged.AddListener(_ => RefreshGridLabels());
        }

        if (menuStartButton != null)
        {
            menuStartButton.onClick.AddListener(ShowSetupScreen);
        }

        if (menuExitButton != null)
        {
            menuExitButton.onClick.AddListener(QuitApplication);
        }

        if (setupBackButton != null)
        {
            setupBackButton.onClick.AddListener(ShowMenuScreen);
        }

        if (setupStartButton != null)
        {
            setupStartButton.onClick.AddListener(BeginConfiguredGame);
        }

        if (builtInButtonA != null)
        {
            builtInButtonA.onClick.AddListener(() =>
            {
                if (builtInImages.Count > 0)
                {
                    SelectSourceImage(builtInImages[0]);
                }
            });
        }

        if (builtInButtonB != null)
        {
            builtInButtonB.onClick.AddListener(() =>
            {
                if (builtInImages.Count > 1)
                {
                    SelectSourceImage(builtInImages[1]);
                }
            });
        }

        if (customImageButton != null)
        {
            customImageButton.onClick.AddListener(LoadCustomImage);
        }

        if (gameExitButton != null)
        {
            gameExitButton.onClick.AddListener(ReturnToMenuFromGame);
        }

        uiWired = true;
    }

    private void CreateBuiltInImages()
    {
        if (builtInImages.Count > 0)
        {
            return;
        }

        if (mountainsTexture != null)
        {
            builtInImages.Add(new PuzzleSourceImage("Горы", PreparePuzzleTexture(mountainsTexture, "BuiltIn_Mountains")));
        }

        if (gladeTexture != null)
        {
            builtInImages.Add(new PuzzleSourceImage("Поляна", PreparePuzzleTexture(gladeTexture, "BuiltIn_Glade")));
        }

        if (builtInImages.Count == 0)
        {
            builtInImages.Add(new PuzzleSourceImage("Закатная долина", PuzzleTextureFactory.CreateSunsetTexture(1200, 1200)));
            builtInImages.Add(new PuzzleSourceImage("Геометрический ритм", PuzzleTextureFactory.CreateGeometryTexture(1200, 1200)));
        }

        ApplyBuiltInButtonLabels();
    }

    private void ApplyBuiltInButtonLabels()
    {
        SetButtonLabel(builtInButtonA, builtInImages.Count > 0 ? builtInImages[0].DisplayName : "Стандарт 1");
        SetButtonLabel(builtInButtonB, builtInImages.Count > 1 ? builtInImages[1].DisplayName : "Стандарт 2");
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        var text = button.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private void RefreshGridLabels()
    {
        if (horizontalValueText != null && horizontalSlider != null)
        {
            horizontalValueText.text = Mathf.RoundToInt(horizontalSlider.value).ToString();
        }

        if (verticalValueText != null && verticalSlider != null)
        {
            verticalValueText.text = Mathf.RoundToInt(verticalSlider.value).ToString();
        }
    }

    private void ShowMenuScreen()
    {
        SetScreenState(true, false, false);
        SetSetupStatus("Выберите «Начать игру», чтобы перейти к настройке пазла.");
    }

    private void ShowSetupScreen()
    {
        SetScreenState(false, true, false);
        SetSetupStatus("Выберите изображение, настройте сетку разрезания и начните игру.");
    }

    private void ShowGameScreen()
    {
        SetScreenState(false, false, true);
    }

    private void SetScreenState(bool showMenu, bool showSetup, bool showGame)
    {
        if (menuCanvas != null)
        {
            menuCanvas.gameObject.SetActive(showMenu);
        }

        if (setupCanvas != null)
        {
            setupCanvas.gameObject.SetActive(showSetup);
        }

        if (gameCanvas != null)
        {
            gameCanvas.gameObject.SetActive(showGame);
        }
    }

    private void SelectSourceImage(PuzzleSourceImage sourceImage)
    {
        selectedTexture = sourceImage.Texture;

        if (previewImage != null)
        {
            previewImage.texture = sourceImage.Texture;
            previewImage.color = Color.white;
        }

        if (boardGuideImage != null)
        {
            boardGuideImage.texture = sourceImage.Texture;
        }

        if (selectedImageText != null)
        {
            selectedImageText.text = $"Изображение: {sourceImage.DisplayName}";
        }

        SetSetupStatus($"Выбрано изображение «{sourceImage.DisplayName}». Теперь задайте размер разрезания.");
    }

    private void LoadCustomImage()
    {
        var path = NativeImagePicker.OpenImageFile();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetSetupStatus("Выбор файла отменён.");
            return;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(path),
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!texture.LoadImage(bytes, false))
            {
                Destroy(texture);
                SetSetupStatus("Не удалось загрузить файл как изображение.", true);
                return;
            }

            DestroyTexture(ref customTexture);
            customTexture = PreparePuzzleTexture(texture, texture.name);
            Destroy(texture);
            SelectSourceImage(new PuzzleSourceImage(texture.name, customTexture));
        }
        catch (Exception exception)
        {
            SetSetupStatus($"Ошибка загрузки изображения: {exception.Message}", true);
        }
    }

    private void BeginConfiguredGame()
    {
        ShowGameScreen();
        if (!StartPuzzle())
        {
            ShowSetupScreen();
        }
    }

    private bool StartPuzzle()
    {
        if (!HasGameBindings())
        {
            SetSetupStatus("Не хватает ссылок на элементы сцены. Проверьте привязки канвасов и игровых областей.", true);
            return false;
        }

        if (selectedTexture == null)
        {
            SetSetupStatus("Сначала выберите изображение.", true);
            return false;
        }

        var columns = horizontalSlider != null ? Mathf.RoundToInt(horizontalSlider.value) : 4;
        var rows = verticalSlider != null ? Mathf.RoundToInt(verticalSlider.value) : 4;

        if (columns < MinGridSize || rows < MinGridSize)
        {
            SetSetupStatus("Размер сетки должен быть не меньше 2x2.", true);
            return false;
        }

        ClearExistingPuzzle();
        puzzleCompleted = false;

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }

        ApplyGameLayout();
        Canvas.ForceUpdateCanvases();
        currentLayout = CalculateLayoutMetrics(rows, columns);
        currentShapes = PuzzlePieceGenerator.GenerateShapes(rows, columns);

        UpdateBoardGuide();

        var startPositions = BuildStartPositions(rows * columns);
        var startIndex = 0;

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var slice = PuzzlePieceGenerator.CalculateSliceBounds(
                    row,
                    column,
                    rows,
                    columns,
                    selectedTexture.width,
                    selectedTexture.height);

                var sprite = PuzzlePieceGenerator.CreatePieceSprite(
                    selectedTexture,
                    slice,
                    currentShapes[row, column],
                    currentLayout.TabMarginSource);

                var pieceObject = new GameObject($"Piece {row}-{column}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(PuzzlePieceView));
                pieceObject.transform.SetParent(piecesLayer, false);

                var pieceRect = (RectTransform)pieceObject.transform;
                pieceRect.sizeDelta = new Vector2(sprite.rect.width * currentLayout.UiScale, sprite.rect.height * currentLayout.UiScale);
                pieceRect.anchoredPosition = startPositions[startIndex++];
                pieceRect.localScale = Vector3.one * sourceAreaPieceScale;

                var pieceImage = pieceObject.GetComponent<Image>();
                pieceImage.sprite = sprite;
                pieceImage.raycastTarget = true;
                pieceImage.preserveAspect = true;
                pieceImage.useSpriteMesh = true;
                pieceImage.alphaHitTestMinimumThreshold = 0.05f;

                var outline = pieceObject.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.28f);
                outline.effectDistance = new Vector2(1.2f, -1.2f);

                var pieceView = pieceObject.GetComponent<PuzzlePieceView>();
                pieceView.Initialize(this, sprite, row, column, GetPieceCorrectPosition(slice));

                var group = new PuzzlePieceGroup(pieceView);
                activePieces.Add(pieceView);
                activeGroups.Add(group);
            }
        }

        SetGameHint($"Сетка {columns}x{rows}. Перетаскивайте детали вокруг поля и собирайте изображение в центре.");
        return true;
    }

    private bool HasGameBindings()
    {
        return sourceArea != null &&
               boardArea != null &&
               piecesLayer != null &&
               boardGuideRect != null &&
               boardGuideImage != null;
    }

    public void BeginDrag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (puzzleCompleted || piece.Group == null || piecesLayer == null)
        {
            return;
        }

        draggingGroup = piece.Group;
        SetGroupScale(draggingGroup, FullPieceScale);
        draggingGroup.BringToFront();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            lastPointerLocalPoint = localPoint;
        }
    }

    public void Drag(PuzzlePieceView piece, PointerEventData eventData)
    {
        if (draggingGroup == null || piece.Group != draggingGroup)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(piecesLayer, eventData.position, eventData.pressEventCamera, out var localPoint))
        {
            return;
        }

        var delta = localPoint - lastPointerLocalPoint;
        draggingGroup.MoveBy(delta);
        lastPointerLocalPoint = localPoint;
    }

    public void EndDrag(PuzzlePieceView piece)
    {
        if (draggingGroup == null || piece.Group != draggingGroup)
        {
            draggingGroup = null;
            return;
        }

        var group = draggingGroup;
        draggingGroup = null;

        var resolvedGroup = ResolvePlacement(group);
        ApplyGroupRestingScale(resolvedGroup);
        EvaluateCompletion();
    }

    public void BeginHover(PuzzlePieceView piece)
    {
        if (puzzleCompleted || piece == null || piece.Group == null || piece.Group == draggingGroup)
        {
            return;
        }

        SetGroupScale(piece.Group, FullPieceScale);
        piece.Group.BringToFront();
    }

    public void EndHover(PuzzlePieceView piece)
    {
        if (puzzleCompleted || piece == null || piece.Group == null || piece.Group == draggingGroup)
        {
            return;
        }

        ApplyGroupRestingScale(piece.Group);
    }

    private PuzzlePieceGroup ResolvePlacement(PuzzlePieceGroup group)
    {
        var currentGroup = group;

        for (var i = 0; i < 12; i++)
        {
            var changed = false;

            if (TrySnapGroupToBoard(currentGroup))
            {
                changed = true;
            }

            if (TryMergeWithNeighbor(currentGroup, out var mergedGroup))
            {
                currentGroup = mergedGroup;
                changed = true;
            }

            if (TrySnapGroupToBoard(currentGroup))
            {
                changed = true;
            }

            if (!changed)
            {
                break;
            }
        }

        return currentGroup;
    }

    private bool TrySnapGroupToBoard(PuzzlePieceGroup group)
    {
        var bestDistance = float.MaxValue;
        var bestDelta = Vector2.zero;

        foreach (var piece in group.Members)
        {
            var delta = piece.CorrectAnchoredPosition - piece.RectTransform.anchoredPosition;
            if (Mathf.Abs(delta.x) > currentLayout.BoardSnapThreshold || Mathf.Abs(delta.y) > currentLayout.BoardSnapThreshold)
            {
                continue;
            }

            var distance = delta.sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestDelta = delta;
            }
        }

        if (bestDistance == float.MaxValue)
        {
            return false;
        }

        group.MoveBy(bestDelta);
        return true;
    }

    private bool TryMergeWithNeighbor(PuzzlePieceGroup movingGroup, out PuzzlePieceGroup mergedGroup)
    {
        var bestDistance = float.MaxValue;
        var bestCorrection = Vector2.zero;
        PuzzlePieceGroup bestTargetGroup = null;

        foreach (var targetGroup in activeGroups)
        {
            if (targetGroup == movingGroup)
            {
                continue;
            }

            foreach (var movingPiece in movingGroup.Members)
            {
                foreach (var targetPiece in targetGroup.Members)
                {
                    if (!AreNeighbors(movingPiece, targetPiece))
                    {
                        continue;
                    }

                    var expectedDelta = targetPiece.CorrectAnchoredPosition - movingPiece.CorrectAnchoredPosition;
                    var correctedPosition = targetPiece.RectTransform.anchoredPosition - expectedDelta;
                    var correction = correctedPosition - movingPiece.RectTransform.anchoredPosition;

                    if (Mathf.Abs(correction.x) > currentLayout.MergeThreshold || Mathf.Abs(correction.y) > currentLayout.MergeThreshold)
                    {
                        continue;
                    }

                    var distance = correction.sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestCorrection = correction;
                        bestTargetGroup = targetGroup;
                    }
                }
            }
        }

        if (bestTargetGroup == null)
        {
            mergedGroup = movingGroup;
            return false;
        }

        movingGroup.MoveBy(bestCorrection);
        bestTargetGroup.Absorb(movingGroup);
        bestTargetGroup.BringToFront();
        activeGroups.Remove(movingGroup);
        mergedGroup = bestTargetGroup;
        return true;
    }

    private void EvaluateCompletion()
    {
        if (activePieces.Count == 0 || activeGroups.Count != 1)
        {
            return;
        }

        foreach (var piece in activePieces)
        {
            if ((piece.RectTransform.anchoredPosition - piece.CorrectAnchoredPosition).sqrMagnitude > 0.25f)
            {
                return;
            }
        }

        puzzleCompleted = true;
        SetGameHint("Поздравляем, пазл собран!");

        SetGroupScale(activeGroups[0], FullPieceScale);

        if (completionText != null)
        {
            completionText.rectTransform.SetAsLastSibling();
            completionText.gameObject.SetActive(true);
        }
    }

    private void ReturnToMenuFromGame()
    {
        ClearExistingPuzzle();
        puzzleCompleted = false;

        if (completionText != null)
        {
            completionText.gameObject.SetActive(false);
        }

        RestoreDefaultLayout();
        ShowMenuScreen();
    }

    private void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyGameLayout()
    {
        if (sourceArea != null)
        {
            sourceArea.anchorMin = sourceAreaDefaultAnchorMin;
            sourceArea.anchorMax = sourceAreaDefaultAnchorMax;
            sourceArea.anchoredPosition = sourceAreaDefaultAnchoredPosition;
            sourceArea.sizeDelta = sourceAreaDefaultSizeDelta;
            sourceArea.gameObject.SetActive(sourceAreaDefaultActive);
        }

        if (boardArea != null)
        {
            boardArea.anchorMin = boardAreaDefaultAnchorMin;
            boardArea.anchorMax = boardAreaDefaultAnchorMax;
            boardArea.anchoredPosition = boardAreaDefaultAnchoredPosition;
            boardArea.sizeDelta = boardAreaDefaultSizeDelta;
        }
    }

    private void RestoreDefaultLayout()
    {
        if (sourceArea != null)
        {
            sourceArea.anchorMin = sourceAreaDefaultAnchorMin;
            sourceArea.anchorMax = sourceAreaDefaultAnchorMax;
            sourceArea.anchoredPosition = sourceAreaDefaultAnchoredPosition;
            sourceArea.sizeDelta = sourceAreaDefaultSizeDelta;
            sourceArea.gameObject.SetActive(sourceAreaDefaultActive);
        }

        if (boardArea != null)
        {
            boardArea.anchorMin = boardAreaDefaultAnchorMin;
            boardArea.anchorMax = boardAreaDefaultAnchorMax;
            boardArea.anchoredPosition = boardAreaDefaultAnchoredPosition;
            boardArea.sizeDelta = boardAreaDefaultSizeDelta;
        }
    }

    private PuzzleLayoutMetrics CalculateLayoutMetrics(int rows, int columns)
    {
        var boardAvailable = boardArea.rect.size - new Vector2(BoardSidePadding * 2f, BoardTopPadding + BoardBottomPadding);
        boardAvailable.x = Mathf.Max(boardAvailable.x, 260f);
        boardAvailable.y = Mathf.Max(boardAvailable.y, 260f);

        var maxCellWidth = Mathf.Ceil(selectedTexture.width / (float)columns);
        var maxCellHeight = Mathf.Ceil(selectedTexture.height / (float)rows);
        var minCellSource = Mathf.Min(selectedTexture.width / (float)columns, selectedTexture.height / (float)rows);
        var tabMarginSource = Mathf.CeilToInt(minCellSource * 0.38f + 4f);
        var maxPieceSourceSize = new Vector2(maxCellWidth + tabMarginSource * 2f, maxCellHeight + tabMarginSource * 2f);
        var pieceGapSource = Mathf.Max(12f, minCellSource * 0.16f);

        var boardScale = Mathf.Min(
            boardAvailable.x / selectedTexture.width,
            boardAvailable.y / selectedTexture.height);
        var uiScale = Mathf.Max(0.06f, boardScale);
        var cellMinUi = minCellSource * uiScale;
        var pieceGapUi = pieceGapSource * uiScale;
        var maxPieceUiSize = maxPieceSourceSize * uiScale;
        var puzzleSize = new Vector2(selectedTexture.width * uiScale, selectedTexture.height * uiScale);
        var boardGuideLocalCenter = new Vector2(0f, (BoardBottomPadding - BoardTopPadding) * 0.5f);

        return new PuzzleLayoutMetrics(
            uiScale,
            puzzleSize,
            maxPieceUiSize,
            pieceGapUi,
            tabMarginSource,
            Mathf.Max(26f, cellMinUi * 0.45f),
            Mathf.Max(22f, cellMinUi * 0.36f),
            boardGuideLocalCenter);
    }

    private void UpdateBoardGuide()
    {
        if (boardGuideRect != null)
        {
            boardGuideRect.anchorMin = new Vector2(0.5f, 0.5f);
            boardGuideRect.anchorMax = new Vector2(0.5f, 0.5f);
            boardGuideRect.pivot = new Vector2(0.5f, 0.5f);
            boardGuideRect.sizeDelta = currentLayout.PuzzleSize;
            boardGuideRect.anchoredPosition = currentLayout.BoardGuideLocalCenter;
            boardGuideRect.SetAsFirstSibling();
        }

        if (boardGuideImage != null)
        {
            boardGuideImage.texture = selectedTexture;
            boardGuideImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private List<Vector2> BuildStartPositions(int count)
    {
        if (sourceArea != null && sourceArea.gameObject.activeInHierarchy)
        {
            return BuildSourceStartPositions(count);
        }

        var positions = new List<Vector2>(count);
        var localPositions = new List<Vector2>(count);
        var innerMin = new Vector2(
            -boardArea.rect.width * 0.5f + BoardSidePadding,
            -boardArea.rect.height * 0.5f + BoardBottomPadding);
        var innerMax = new Vector2(
            boardArea.rect.width * 0.5f - BoardSidePadding,
            boardArea.rect.height * 0.5f - BoardTopPadding);
        var inflatedGuideHalf = currentLayout.PuzzleSize * 0.5f + currentLayout.MaxPieceUiSize * 0.52f;
        var spawnPieceSize = currentLayout.MaxPieceUiSize * sourceAreaPieceScale;
        var minSeparation = Mathf.Max(
            Mathf.Min(spawnPieceSize.x, spawnPieceSize.y) * 0.72f,
            28f);
        var halfPieceSize = spawnPieceSize * 0.5f;

        for (var i = 0; i < count; i++)
        {
            var placed = false;

            for (var attempt = 0; attempt < 220 && !placed; attempt++)
            {
                var candidate = new Vector2(
                    UnityEngine.Random.Range(innerMin.x + halfPieceSize.x, innerMax.x - halfPieceSize.x),
                    UnityEngine.Random.Range(innerMin.y + halfPieceSize.y, innerMax.y - halfPieceSize.y));

                var relativeToGuide = candidate - currentLayout.BoardGuideLocalCenter;
                var outsideGuide = Mathf.Abs(relativeToGuide.x) > inflatedGuideHalf.x + SpawnSafetyPadding ||
                                   Mathf.Abs(relativeToGuide.y) > inflatedGuideHalf.y + SpawnSafetyPadding;
                if (!outsideGuide)
                {
                    continue;
                }

                var relaxedSeparation = Mathf.Lerp(minSeparation, minSeparation * 0.35f, attempt / 219f);
                var overlaps = false;
                for (var positionIndex = 0; positionIndex < localPositions.Count; positionIndex++)
                {
                    if ((localPositions[positionIndex] - candidate).sqrMagnitude < relaxedSeparation * relaxedSeparation)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    continue;
                }

                localPositions.Add(candidate);
                placed = true;
            }

            if (!placed)
            {
                var angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var fallback = currentLayout.BoardGuideLocalCenter +
                               direction *
                               Mathf.Max(currentLayout.PuzzleSize.x, currentLayout.PuzzleSize.y) * 0.82f;
                fallback.x = Mathf.Clamp(fallback.x, innerMin.x + halfPieceSize.x, innerMax.x - halfPieceSize.x);
                fallback.y = Mathf.Clamp(fallback.y, innerMin.y + halfPieceSize.y, innerMax.y - halfPieceSize.y);
                localPositions.Add(fallback);
            }
        }

        for (var i = localPositions.Count - 1; i > 0; i--)
        {
            var swapIndex = UnityEngine.Random.Range(0, i + 1);
            (localPositions[i], localPositions[swapIndex]) = (localPositions[swapIndex], localPositions[i]);
        }

        for (var i = 0; i < localPositions.Count; i++)
        {
            positions.Add(ConvertLocalPoint(boardArea, piecesLayer, localPositions[i]));
        }

        return positions;
    }

    private List<Vector2> BuildSourceStartPositions(int count)
    {
        var positions = new List<Vector2>(count);
        var localPositions = new List<Vector2>(count);
        var spawnPieceSize = currentLayout.MaxPieceUiSize * sourceAreaPieceScale;
        var halfPieceSize = spawnPieceSize * 0.5f;
        var sidePadding = Mathf.Max(24f, halfPieceSize.x * 0.32f);
        var topPadding = Mathf.Max(86f, halfPieceSize.y * 0.75f);
        var bottomPadding = Mathf.Max(34f, halfPieceSize.y * 0.45f);
        var innerMin = new Vector2(
            -sourceArea.rect.width * 0.5f + sidePadding,
            -sourceArea.rect.height * 0.5f + bottomPadding);
        var innerMax = new Vector2(
            sourceArea.rect.width * 0.5f - sidePadding,
            sourceArea.rect.height * 0.5f - topPadding);
        var minimumSeparation = Mathf.Max(
            Mathf.Min(spawnPieceSize.x, spawnPieceSize.y) * 0.32f,
            16f);

        for (var i = 0; i < count; i++)
        {
            var placed = false;

            for (var attempt = 0; attempt < 180 && !placed; attempt++)
            {
                var candidate = new Vector2(
                    UnityEngine.Random.Range(innerMin.x, innerMax.x),
                    UnityEngine.Random.Range(innerMin.y, innerMax.y));

                var relaxedSeparation = Mathf.Lerp(minimumSeparation, minimumSeparation * 0.15f, attempt / 179f);
                var overlaps = false;
                for (var positionIndex = 0; positionIndex < localPositions.Count; positionIndex++)
                {
                    if ((localPositions[positionIndex] - candidate).sqrMagnitude < relaxedSeparation * relaxedSeparation)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    continue;
                }

                localPositions.Add(candidate);
                placed = true;
            }

            if (!placed)
            {
                localPositions.Add(new Vector2(
                    UnityEngine.Random.Range(innerMin.x, innerMax.x),
                    UnityEngine.Random.Range(innerMin.y, innerMax.y)));
            }
        }

        for (var i = localPositions.Count - 1; i > 0; i--)
        {
            var swapIndex = UnityEngine.Random.Range(0, i + 1);
            (localPositions[i], localPositions[swapIndex]) = (localPositions[swapIndex], localPositions[i]);
        }

        for (var i = 0; i < localPositions.Count; i++)
        {
            positions.Add(ConvertLocalPoint(sourceArea, piecesLayer, localPositions[i]));
        }

        return positions;
    }

    private Vector2 GetPieceCorrectPosition(PuzzleSliceBounds slice)
    {
        var centerX = (slice.XMin + slice.XMax) * 0.5f - selectedTexture.width * 0.5f;
        var centerY = (slice.YMin + slice.YMax) * 0.5f - selectedTexture.height * 0.5f;
        var referenceRect = boardGuideRect != null ? boardGuideRect : boardArea;
        return ConvertLocalPoint(referenceRect, piecesLayer, new Vector2(centerX * currentLayout.UiScale, centerY * currentLayout.UiScale));
    }

    private void ApplyGroupRestingScale(PuzzlePieceGroup group)
    {
        if (group == null)
        {
            return;
        }

        SetGroupScale(group, IsGroupInSourceArea(group) ? sourceAreaPieceScale : FullPieceScale);
    }

    private void SetGroupScale(PuzzlePieceGroup group, float scale)
    {
        if (group == null)
        {
            return;
        }

        var clampedScale = Mathf.Clamp(scale, 0.1f, FullPieceScale);
        foreach (var member in group.Members)
        {
            if (member == null)
            {
                continue;
            }

            member.RectTransform.localScale = Vector3.one * clampedScale;
        }
    }

    private bool IsGroupInSourceArea(PuzzlePieceGroup group)
    {
        if (group == null || sourceArea == null || !sourceArea.gameObject.activeInHierarchy)
        {
            return false;
        }

        foreach (var member in group.Members)
        {
            if (member == null)
            {
                continue;
            }

            var localPoint = sourceArea.InverseTransformPoint(member.RectTransform.position);
            if (sourceArea.rect.Contains(localPoint))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearExistingPuzzle()
    {
        foreach (var piece in activePieces)
        {
            if (piece == null)
            {
                continue;
            }

            var sprite = piece.PieceSprite;
            if (sprite != null)
            {
                var texture = sprite.texture;
                Destroy(sprite);
                if (texture != null)
                {
                    Destroy(texture);
                }
            }

            Destroy(piece.gameObject);
        }

        activePieces.Clear();
        activeGroups.Clear();
    }

    private void SetSetupStatus(string message, bool isError = false)
    {
        if (setupStatusText == null)
        {
            return;
        }

        setupStatusText.color = Color.black;
        setupStatusText.text = message;
    }

    private void SetGameHint(string message)
    {
        if (gameHintText != null)
        {
            gameHintText.text = message;
        }
    }

    private static bool AreNeighbors(PuzzlePieceView first, PuzzlePieceView second)
    {
        return Mathf.Abs(first.Row - second.Row) + Mathf.Abs(first.Column - second.Column) == 1;
    }

    private static Vector2 ConvertLocalPoint(RectTransform from, RectTransform to, Vector2 localPoint)
    {
        var worldPoint = from.TransformPoint(localPoint);
        return to.InverseTransformPoint(worldPoint);
    }

    private static void DestroyTexture(ref Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        Destroy(texture);
        texture = null;
    }

    private Texture2D PreparePuzzleTexture(Texture sourceTexture, string textureName)
    {
        var prepared = PuzzleTextureFactory.PrepareReadableTexture(sourceTexture, 1600, textureName);
        prepared.filterMode = FilterMode.Bilinear;
        prepared.wrapMode = TextureWrapMode.Clamp;
        return prepared;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        TryAssignBuiltInTexture(ref mountainsTexture, "Горы");
        TryAssignBuiltInTexture(ref gladeTexture, "Поляна");
    }

    private static void TryAssignBuiltInTexture(ref Texture2D texture, string assetName)
    {
        if (texture != null)
        {
            return;
        }

        var guids = AssetDatabase.FindAssets($"{assetName} t:Texture2D", new[] { "Assets" });
        if (guids.Length == 0)
        {
            return;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
#endif
}
