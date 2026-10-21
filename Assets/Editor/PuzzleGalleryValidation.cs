#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Video;
using Object = UnityEngine.Object;

// Runs real scene interactions in Play Mode and saves reviewable renders under Logs.
[InitializeOnLoad]
internal static class PuzzleGalleryValidation
{
    private const string SessionKey = "TileVerse.GalleryValidation";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly List<string> checks = new();
    private static readonly List<string> performance = new();
    private static double playbackRequestedAt;
    private static string firstFrameLabel;
    private static int preparedTargetId;
    private static PuzzleGameController controller;
    private static Camera camera;
    private static RenderTexture captureTarget;
    private static int stage;
    private static double nextUpdate;
    private static double deadline;
    private static double pausedVideoTime;
    private static bool previousRunInBackground;
    private static string Output => Path.GetFullPath("Logs/GalleryPreview");

    static PuzzleGalleryValidation()
    {
        EditorApplication.playModeStateChanged += HandlePlayMode;
    }

    [MenuItem("Tools/TileVerse/Validate gallery in Play Mode")]
    public static void Run() => StartValidation(true);

    public static void RunAutomated() => StartValidation(false);

    private static void StartValidation(bool prompt)
    {
        if (prompt && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        SessionState.SetBool(SessionKey, true);
        SessionState.SetInt(SessionKey + ".result", 1);
        SessionState.SetBool(SessionKey + ".exitPending", false);
        SessionState.SetBool(SessionKey + ".exitFrameIntact", false);
        Directory.CreateDirectory(Output);
        EditorApplication.isPlaying = true;
    }

    private static void HandlePlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(SessionKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            stage = 0;
            checks.Clear();
            performance.Clear();
            nextUpdate = EditorApplication.timeSinceStartup + 2;
            deadline = nextUpdate + 90;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(SessionKey + ".exitPending", false))
        {
            var ending = controller != null ? Get<PuzzleCompletionVideo>(controller, "completionVideo") : null;
            var visible = ending != null ? Get<RawImage>(ending, "overlay") : null;
            SessionState.SetBool(SessionKey + ".exitFrameIntact", visible != null && visible.enabled && visible.texture != null &&
                visible.texture == Get<RenderTexture>(ending, "runtimeTexture") && Get<CanvasGroup>(ending, "overlayGroup").alpha > 0.99f);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            if (SessionState.GetBool(SessionKey + ".exitPending", false))
            {
                SessionState.SetBool(SessionKey + ".exitPending", false);
                if (SessionState.GetBool(SessionKey + ".exitFrameIntact", false))
                {
                    Check("Exit keeps the video frame intact until scene teardown", true);
                    Check("Settings Exit button stops Play Mode in the editor", true);
                    Finish(true, "All gallery checks passed.");
                }
                else Finish(false, "Exit cleared the video before scene teardown.");
            }
            SessionState.SetBool(SessionKey, false);
            if (Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt(SessionKey + ".result", 1));
        }
    }

    private static void Tick()
    {
        // Batch validation has no visible Game view to drive coroutines.
        if (Application.isBatchMode)
        {
            EditorApplication.isPaused = false;
            EditorApplication.QueuePlayerLoopUpdate();
        }
        if (EditorApplication.timeSinceStartup < nextUpdate) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Validation timed out at stage " + stage + ".");
            switch (stage)
            {
                case 0:
                    ValidateScene();
                    stage = 1;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.25;
                    break;
                case 1:
                    var video = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    var overlay = Get<RawImage>(video, "overlay");
                    if (!overlay.enabled || Get<CanvasGroup>(video, "overlayGroup").alpha <= 0f) break;
                    Check("Video decodes and fades in", Get<CanvasGroup>(video, "overlayGroup").alpha > 0f);
                    var target = Get<RenderTexture>(video, "runtimeTexture");
                    var guide = Get<RectTransform>(controller, "boardGuideRect");
                    Check("Video target follows picture aspect", Mathf.Abs(target.width / (float)target.height - guide.rect.width / guide.rect.height) < 0.01f);
                    Check("Video fills the picture with a proportional crop", Get<VideoPlayer>(video, "videoPlayer").aspectRatio == VideoAspectRatio.FitOutside);
                    Check("Video letterbox has a black matte", GameObject.Find("Video Matte").GetComponent<Image>().color == Color.black);
                    Invoke(controller, "OpenSettings");
                    pausedVideoTime = Get<VideoPlayer>(video, "videoPlayer").time;
                    stage = 2;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.5;
                    break;
                case 2:
                    var player = Get<VideoPlayer>(Get<PuzzleCompletionVideo>(controller, "completionVideo"), "videoPlayer");
                    Check("Esc settings pause playback", !player.isPlaying && Math.Abs(player.time - pausedVideoTime) < 0.05);
                    Invoke(controller, "CloseSettings", false);
                    stage = 3;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.5;
                    break;
                case 3:
                    var resumed = Get<VideoPlayer>(Get<PuzzleCompletionVideo>(controller, "completionVideo"), "videoPlayer");
                    if (!resumed.isPlaying || resumed.time <= pausedVideoTime) break;
                    Check("Closing settings resumes playback", resumed.isPlaying && resumed.time > pausedVideoTime);
                    Capture("completion-video.png");
                    var presentation = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    var invalid = new PuzzleMediaEntry("Invalid video", Get<PuzzleMediaEntry>(controller, "activeMedia").Image);
                    invalid.SetCustomVideoPath(Path.Combine(Output, "missing-video.mp4"));
                    presentation.Play(invalid);
                    stage = 4;
                    nextUpdate = EditorApplication.timeSinceStartup + 1;
                    break;
                case 4:
                    var failed = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    if (!Get<TMP_Text>(failed, "statusText").gameObject.activeSelf) break;
                    Check("Invalid video keeps completed photo", !Get<RawImage>(failed, "overlay").enabled && Get<RawImage>(controller, "boardGuideImage").color.a == 1f);
                    Check("Invalid video shows fallback message", Get<TMP_Text>(failed, "statusText").gameObject.activeSelf);
                    var messageRect = (RectTransform)Get<GameObject>(failed, "statusPanel").transform;
                    var pictureRect = Get<RectTransform>(controller, "boardGuideRect");
                    Check("Video message stays inside portrait picture", messageRect.rect.height <= pictureRect.rect.height && messageRect.rect.width <= pictureRect.rect.width);
                    Capture("video-fallback.png");
                    var timeoutMedia = Get<PuzzleMediaEntry>(controller, "activeMedia").Copy();
                    timeoutMedia.SetCustomVideoPath(Path.GetFullPath("Tests/Fixtures/gallery-video.mp4"));
                    Set(failed, "prepareTimeout", 0.3f);
                    failed.Play(timeoutMedia);
                    Get<VideoPlayer>(failed, "videoPlayer").sendFrameReadyEvents = false;
                    stage = 5;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.5;
                    break;
                case 5:
                    var timedOut = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    if (!Get<TMP_Text>(timedOut, "statusText").gameObject.activeSelf) break;
                    Check("Missing decoded frame falls back after timeout", !Get<RawImage>(timedOut, "overlay").enabled && Get<RawImage>(controller, "boardGuideImage").color.a == 1f);
                    Set(timedOut, "prepareTimeout", 15f);
                    var defaultPlayback = PuzzleMediaEntry.LoadDefault();
                    Set(defaultPlayback, "loopVideo", true);
                    Set(controller, "activeMedia", defaultPlayback);
                    Invoke(controller, "StartConfiguredPuzzle");
                    var defaultPieces = Get<List<PuzzlePieceView>>(controller, "activePieces");
                    foreach (var piece in defaultPieces) piece.RectTransform.anchoredPosition = piece.CorrectAnchoredPosition;
                    foreach (var piece in defaultPieces) Invoke(controller, "ResolvePlacement", piece.Group);
                    Invoke(controller, "EvaluateCompletion");
                    stage = 6;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.5;
                    break;
                case 6:
                    var defaultVideo = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    var defaultStatus = Get<TMP_Text>(defaultVideo, "statusText");
                    if (defaultStatus.gameObject.activeSelf) throw new Exception("Default video failed: " + defaultStatus.text);
                    if (!Get<RawImage>(defaultVideo, "overlay").enabled || Get<CanvasGroup>(defaultVideo, "overlayGroup").alpha < 0.99f) break;
                    var defaultPlayer = Get<VideoPlayer>(defaultVideo, "videoPlayer");
                    Check("Default ispu_1 clip decodes after puzzle completion", defaultPlayer.source == VideoSource.VideoClip && defaultPlayer.clip.name == "ispu_1" && defaultPlayer.isPlaying);
                    CheckDefaultVideoEdges(Get<RenderTexture>(defaultVideo, "runtimeTexture"));
                    Capture("completion-ispu.png");
                    BeginWarmVideoValidation();
                    stage = 7;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.05;
                    break;
                case 7:
                    var warm = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    var warmPlayer = Get<VideoPlayer>(warm, "videoPlayer");
                    if (!warmPlayer.isPrepared) break;
                    Check("Near-completion preparation stays silent and hidden", !warmPlayer.isPlaying && !Get<RawImage>(warm, "overlay").enabled);
                    Check("Prepared video has no polling coroutine", Get<Coroutine>(warm, "loadRoutine") == null);
                    preparedTargetId = Get<RenderTexture>(warm, "runtimeTexture").GetInstanceID();
                    warm.Prepare(Get<PuzzleMediaEntry>(controller, "activeMedia"));
                    Check("Repeated preparation reuses the ready player and texture", warmPlayer.isPrepared && Get<RenderTexture>(warm, "runtimeTexture").GetInstanceID() == preparedTargetId);
                    Invoke(controller, "OpenSettings");
                    warmPlayer.frameReady += RecordFirstFrame;
                    firstFrameLabel = "Warm fixture";
                    playbackRequestedAt = EditorApplication.timeSinceStartup;
                    var remaining = Get<List<PuzzlePieceView>>(controller, "activePieces");
                    foreach (var piece in remaining) piece.RectTransform.anchoredPosition = piece.CorrectAnchoredPosition;
                    foreach (var piece in remaining) Invoke(controller, "ResolvePlacement", piece.Group);
                    Invoke(controller, "EvaluateCompletion");
                    Check("Prepared completion preserves menu pause and render target", !warmPlayer.isPlaying && Get<RenderTexture>(warm, "runtimeTexture").GetInstanceID() == preparedTargetId);
                    Invoke(controller, "CloseSettings", false);
                    Check("Completed photo is displayed before prepared playback starts", !warmPlayer.isPlaying && !Get<RawImage>(warm, "overlay").enabled && Get<RawImage>(controller, "boardGuideImage").color.a == 1f);
                    stage = 8;
                    break;
                case 8:
                    var ready = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    if (!Get<RawImage>(ready, "overlay").enabled || Get<CanvasGroup>(ready, "overlayGroup").alpha < 0.99f) break;
                    Check("Prepared video starts after completion", Get<VideoPlayer>(ready, "videoPlayer").isPlaying);
                    Check("Completion restores the gentle 0.45-second fade", Mathf.Abs(Get<float>(ready, "fadeDuration") - 0.45f) < 0.001f);
                    Check("Completion releases generated piece sprites and textures", Get<List<PuzzlePieceView>>(controller, "activePieces").TrueForAll(piece => piece.PieceSprite == null && piece.GetComponent<Image>().sprite == null));
                    SetCaptureSize(1600, 900);
                    CheckBoardAspect(1280f / 855f);
                    Check("Completed picture resizes after releasing piece artwork", Get<bool>(controller, "puzzleCompleted"));
                    ready.Prepare(PuzzleMediaEntry.LoadDefault());
                    ready.StopPlayback();
                    stage = 9;
                    nextUpdate = EditorApplication.timeSinceStartup + 0.5;
                    break;
                case 9:
                    var cancelled = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    var cancelledPlayer = Get<VideoPlayer>(cancelled, "videoPlayer");
                    Check("Cancellation releases video resources and ignores late preparation", !cancelledPlayer.isPrepared && !cancelledPlayer.isPlaying && Get<RenderTexture>(cancelled, "runtimeTexture") == null && Get<PuzzleMediaEntry>(cancelled, "preparedMedia") == null);
                    var unavailable = new PuzzleMediaEntry("Unavailable", Get<PuzzleMediaEntry>(controller, "activeMedia").Image);
                    unavailable.SetCustomVideoPath(Path.Combine(Output, "missing-video.mp4"));
                    cancelled.Prepare(unavailable);
                    Check("Unavailable preloaded video does not interrupt puzzle with a message", !Get<TMP_Text>(cancelled, "statusText").gameObject.activeSelf);
                    cancelled.Play(PuzzleMediaEntry.LoadDefault());
                    stage = 10;
                    break;
                case 10:
                    var exitingVideo = Get<PuzzleCompletionVideo>(controller, "completionVideo");
                    if (!Get<RawImage>(exitingVideo, "overlay").enabled || Get<CanvasGroup>(exitingVideo, "overlayGroup").alpha < 0.99f) break;
                    Invoke(controller, "OpenSettings");
                    Capture("video-before-exit.png");
                    stage = 11;
                    deadline = EditorApplication.timeSinceStartup + 10;
                    SessionState.SetBool(SessionKey + ".exitPending", true);
                    Get<PuzzleSettingsView>(controller, "settingsView").ExitButton.onClick.Invoke();
                    break;
            }
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void ValidateScene()
    {
        controller = Object.FindFirstObjectByType<PuzzleGameController>();
        Check("Scene controller exists", controller != null);
        SetCaptureSize(1920, 1080);
        MeasureGeneration();
        var pieces = Get<List<PuzzlePieceView>>(controller, "activePieces");
        Check("Game caps rendering at 60 FPS", Application.targetFrameRate == 60);
        Check("Generated photo releases its CPU pixel copy", !Get<Texture2D>(controller, "selectedTexture").isReadable);
        var hitImage = pieces[0].GetComponent<Image>();
        var hitRect = pieces[0].RectTransform;
        Check("Piece textures retain readable alpha hit testing", pieces[0].PieceSprite.texture.isReadable &&
            !hitImage.IsRaycastLocationValid(RectTransformUtility.WorldToScreenPoint(camera, hitRect.TransformPoint(hitRect.rect.min + Vector2.one)), camera) &&
            hitImage.IsRaycastLocationValid(RectTransformUtility.WorldToScreenPoint(camera, hitRect.position), camera));
        Check("Startup creates playable classic puzzle", pieces.Count == 16 && Get<PuzzlePieceMode>(controller, "activePieceMode") == PuzzlePieceMode.Classic);
        var defaultEntry = Get<PuzzleMediaEntry>(controller, "activeMedia");
        Check("Startup uses default ispu_1 photo and video", defaultEntry.Image == Resources.Load<Texture2D>("ispu_1") && defaultEntry.VideoClip == Resources.Load<VideoClip>("ispu_1"));
        Check("Default photo retains its original aspect", Mathf.Abs(defaultEntry.Image.width / (float)defaultEntry.Image.height - 1280f / 855f) < 0.001f);
        Check("Legacy sample artwork is not configured", !controller.CopyConfiguredMedia().Exists(entry => entry.DisplayName == "Горы" || entry.DisplayName == "Поляна"));
        var shapes = Get<PuzzlePieceShape[,]>(controller, "currentShapes");
        for (var row = 0; row < 4; row++)
        for (var col = 0; col < 4; col++)
        {
            if (col < 3) Check("Classic horizontal seam matches", shapes[row, col].Right == -shapes[row, col + 1].Left);
            if (row < 3) Check("Classic vertical seam matches", shapes[row, col].Bottom == -shapes[row + 1, col].Top);
        }
        CheckSpawnBounds();
        var settings = Get<PuzzleSettingsView>(controller, "settingsView");
        Invoke(controller, "ClearHover");
        var sample = pieces[0];
        var looseScale = sample.RectTransform.localScale;
        var loosePosition = sample.RectTransform.anchoredPosition;
        controller.BeginHover(sample);
        Check("Hover enlarges a loose piece to assembly size", sample.RectTransform.localScale == Vector3.one && sample.RectTransform.localScale.x > looseScale.x);
        controller.BeginHover(sample);
        Check("Repeated hover entry does not change the piece position", sample.RectTransform.anchoredPosition == loosePosition);
        controller.EndHover(sample);
        Check("Pointer exit restores loose-piece scale", sample.RectTransform.localScale == looseScale);
        controller.BeginHover(sample);
        var grabbed = Vector2.Scale(sample.RectTransform.rect.size, new Vector2(0.1f, 0.08f));
        var pointer = PiecePointer(sample, grabbed);
        controller.BeginDrag(sample, pointer);
        Check("Pickup displays full-size piece", sample.RectTransform.localScale == Vector3.one);
        var grabbedScreen = RectTransformUtility.WorldToScreenPoint(camera, sample.RectTransform.TransformPoint(grabbed));
        Check("Pickup preserves the grabbed point under the pointer", (grabbedScreen - pointer.position).sqrMagnitude < 0.25f);
        Set(controller, "draggingGroup", null);
        Set(controller, "draggedPiece", null);
        sample.RectTransform.anchoredPosition = loosePosition;
        sample.Group.SetHighlighted(false);
        Invoke(controller, "ApplyGroupRestingScale", sample.Group);
        CheckLoosePieceEdgeDragging(pieces);
        var frameView = Get<PuzzleFrameView>(controller, "frameView");
        var frameImage = controller.GetComponent<PuzzleOrnateFrameView>().FrameImage;
        Check("Frame tiles the original rails with transparent center", frameImage.type == Image.Type.Tiled && !frameImage.fillCenter && frameImage.sprite.border == Vector4.one * 320f);
        var inset = (frameView.FrameRoot.rect.width - frameView.ContentRoot.rect.width) * 0.5f;
        Check("Corner and rail proportions match source", Mathf.Abs(320f / frameImage.pixelsPerUnitMultiplier - inset * (320f / 192f)) < 0.01f);
        var originalContentSize = frameView.ContentRoot.rect.size;
        frameView.Apply(PuzzleFrameStyle.Default, new Vector2(1000f, 35f));
        Canvas.ForceUpdateCanvases();
        Check("Panorama corners keep both dimensions", frameImage.rectTransform.rect.height + 0.01f >= 640f / frameImage.pixelsPerUnitMultiplier);
        frameView.Apply(PuzzleFrameStyle.Default, new Vector2(35f, 1000f));
        Canvas.ForceUpdateCanvases();
        Check("Narrow portrait corners keep both dimensions", frameImage.rectTransform.rect.width + 0.01f >= 640f / frameImage.pixelsPerUnitMultiplier);
        frameView.Apply(PuzzleFrameStyle.Default, originalContentSize);
        Canvas.ForceUpdateCanvases();
        Check("No colored frame backing layers", GameObject.Find("Frame Template") == null && GameObject.Find("Inner Shadow") == null && GameObject.Find("Gold Trim") == null && GameObject.Find("Mat") == null && frameView.ContentRoot.GetComponent<Image>() == null);
        Check("No legacy UI Text in scene", Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0);
        Check("TMP font supports Russian labels", settings.ImageName.font.HasCharacters("НастройкиСтолбцыСтрокиФотоВидеоСвойцветПредосмотрЗакреплятьврамкеЁё", out uint[] _, false, true));
        Capture("gameplay-black.png");
        var photo = Get<RawImage>(controller, "boardGuideImage");
        var photoColor = photo.color;
        photo.color = Color.white;
        var background = GameObject.Find("Background").GetComponent<Image>();
        background.color = Color.white;
        foreach (var piece in pieces) piece.GetComponent<Image>().enabled = false;
        Capture("photo-frame-white.png");
        foreach (var piece in pieces) piece.GetComponent<Image>().enabled = true;
        photo.color = photoColor;
        background.color = Color.black;
        var firstId = pieces[0].GetInstanceID();
        Invoke(controller, "OpenSettings");
        Check("Settings are larger than the previous revision", settings.Panel.rect.height * settings.Panel.localScale.y > 900f);
        Check("Preview and placement lock default to enabled", settings.PreviewToggle.isOn && settings.LockPlacedToggle.isOn);
        Capture("settings-black.png");
        settings.PreviewToggle.isOn = false;
        Check("Preview disables only the photo hint without restarting", photo.color.a == 0f && pieces[0].GetInstanceID() == firstId && settings.Preview.texture != null);
        settings.LockPlacedToggle.isOn = false;
        Check("Placement lock changes live without restarting", !controller.LockPlacedPieces && pieces[0].GetInstanceID() == firstId);
        Invoke(controller, "CloseSettings", false);
        Capture("preview-off.png");
        Invoke(controller, "OpenSettings");
        Check("Closing settings preserves live gameplay preferences", !settings.PreviewToggle.isOn && !settings.LockPlacedToggle.isOn && photo.color.a == 0f);
        settings.PreviewToggle.isOn = true;
        settings.LockPlacedToggle.isOn = true;
        Check("Preview restores the faint photo hint", photo.color.a > 0f && photo.color.a < 1f);
        settings.PaletteButtons[7].onClick.Invoke();
        Check("White background applies without reset", GameObject.Find("Background").GetComponent<Image>().color == Color.white && pieces[0].GetInstanceID() == firstId);
        Capture("settings-white.png");
        settings.PaletteButtons[0].onClick.Invoke();
        Check("Black background is exact", GameObject.Find("Background").GetComponent<Image>().color == Color.black);
        settings.PaletteButtons[2].onClick.Invoke();
        Check("Palette applies live without resetting pieces", GameObject.Find("Background").GetComponent<Image>().color == settings.PaletteColors[2] && pieces[0].GetInstanceID() == firstId);
        settings.CustomColorButton.onClick.Invoke();
        Check("Custom color picker opens", settings.ColorPicker.IsOpen);
        var closeLabel = settings.ColorPicker.CloseButton.GetComponentInChildren<TMP_Text>();
        Check("Picker close glyph fits its button", closeLabel.preferredHeight <= closeLabel.rectTransform.rect.height);
        settings.ColorPicker.SubmitHex("#3C5870");
        ColorUtility.TryParseHtmlString("#3C5870", out var custom);
        var actualColor = GameObject.Find("Background").GetComponent<Image>().color;
        Check("HEX color applies live", ((Vector4)(actualColor - custom)).sqrMagnitude < 0.0001f);
        settings.ColorPicker.SubmitHex("invalid");
        Check("Invalid HEX preserves background", GameObject.Find("Background").GetComponent<Image>().color == actualColor && settings.ColorPicker.Error.gameObject.activeSelf);
        ClickColorSurface(settings.ColorPicker.HueBar.rectTransform, new Vector2(0.5f, 0.5f), settings.Canvas);
        ClickColorSurface(settings.ColorPicker.SaturationValue.rectTransform, new Vector2(0.6f, 0.7f), settings.Canvas);
        var hsv = Color.HSVToRGB(0.5f, 0.6f, 0.7f);
        Check("Color surfaces respond to pointer coordinates", ((Vector4)(GameObject.Find("Background").GetComponent<Image>().color - hsv)).sqrMagnitude < 0.001f);
        Capture("color-picker.png");
        Check("Esc closes picker before settings", settings.CloseTransient() && !settings.ColorPicker.IsOpen && settings.Canvas.gameObject.activeSelf);
        settings.PaletteButtons[0].onClick.Invoke();
        settings.TilesButton.onClick.Invoke();
        Invoke(controller, "CloseSettings", false);
        Check("Closing settings discards pending form", Get<PuzzlePieceMode>(controller, "activePieceMode") == PuzzlePieceMode.Classic && pieces[0].GetInstanceID() == firstId);
        Invoke(controller, "OpenSettings");
        settings.TilesButton.onClick.Invoke();
        settings.ApplyButton.onClick.Invoke();
        Check("Form change requests restart", settings.Confirmation.activeSelf);
        settings.CancelButton.onClick.Invoke();
        Check("Restart cancellation preserves puzzle", pieces[0].GetInstanceID() == firstId);
        settings.ApplyButton.onClick.Invoke();
        settings.ConfirmButton.onClick.Invoke();
        Check("Confirmed form change creates tiles", Get<PuzzlePieceMode>(controller, "activePieceMode") == PuzzlePieceMode.Tiles && pieces[0].GetInstanceID() != firstId);
        foreach (var piece in pieces)
        {
            var slice = PuzzlePieceGenerator.CalculateSliceBounds(piece.Row, piece.Column, 4, 4,  Get<Texture2D>(controller, "selectedTexture").width, Get<Texture2D>(controller, "selectedTexture").height);
            Check("Tile has no tab padding", piece.PieceSprite.rect.width == slice.Width && piece.PieceSprite.rect.height == slice.Height);
        }
        Capture("gameplay-tiles.png");

        Invoke(controller, "OpenSettings");
        settings.ColumnsInput.text = "99"; settings.NormalizeGrid();
        Check("Grid entry clamps upper bound", settings.Columns == 8 && !settings.ColumnsPlus.interactable);
        settings.ColumnsInput.text = "-1"; settings.NormalizeGrid();
        Check("Grid entry clamps lower bound", settings.Columns == 2 && !settings.ColumnsMinus.interactable);
        settings.ColumnsPlus.onClick.Invoke();
        Check("Grid plus button changes exact value", settings.Columns == 3);
        settings.ColumnsMinus.onClick.Invoke();
        settings.RowsInput.text = "3"; settings.NormalizeGrid();
        var portrait = new Texture2D(300, 600, TextureFormat.RGBA32, false);
        var pixels = new Color[300 * 600];
        for (var i = 0; i < pixels.Length; i++) pixels[i] = Color.Lerp(new Color(0.10f, 0.22f, 0.3f), new Color(0.82f, 0.64f, 0.38f), (i / 300) / 600f);
        portrait.SetPixels(pixels); portrait.Apply();
        Set(controller, "pendingMedia", new PuzzleMediaEntry("Портретный формат", portrait) { IsRuntimeImage = true });
        settings.ApplyButton.onClick.Invoke();
        settings.ConfirmButton.onClick.Invoke();
        Check("Grid controls change piece count", pieces.Count == 6);
        CheckBoardAspect(0.5f);
        var first = pieces[0]; var second = pieces[1];
        first.RectTransform.anchoredPosition = first.CorrectAnchoredPosition;
        second.RectTransform.anchoredPosition = second.CorrectAnchoredPosition;
        Invoke(controller, "ResolvePlacement", first.Group);
        Check("Neighbor tiles merge", first.Group == second.Group && first.Group.Members.Count == 2);
        Check("Correctly placed fragment records placement", first.Group.IsPlaced);
        var placedPointer = PiecePointer(first, Vector2.zero);
        controller.BeginDrag(first, placedPointer);
        Check("Enabled lock prevents dragging placed fragments", Get<PuzzlePieceGroup>(controller, "draggingGroup") == null && first.RectTransform.anchoredPosition == first.CorrectAnchoredPosition);
        settings.LockPlacedToggle.isOn = false;
        var originalPositions = pieces.ConvertAll(piece => piece.RectTransform.anchoredPosition);
        for (var i = 2; i < pieces.Count; i++) pieces[i].RectTransform.anchoredPosition = new Vector2(750f, -400f);
        var groupGrab = Vector2.Scale(first.RectTransform.rect.size, new Vector2(0.12f, 0.08f));
        controller.BeginHover(first);
        placedPointer = PiecePointer(first, groupGrab);
        controller.BeginDrag(first, placedPointer);
        Check("Disabled lock allows dragging placed fragments", Get<PuzzlePieceGroup>(controller, "draggingGroup") == first.Group && !first.Group.IsPlaced);
        Check("Pickup from an enlarged group preserves the grabbed point", (RectTransformUtility.WorldToScreenPoint(camera, first.RectTransform.TransformPoint(groupGrab)) - placedPointer.position).sqrMagnitude < 0.25f);
        var freeDrop = first.RectTransform.anchoredPosition + new Vector2(48f, 36f);
        var freeLayer = Get<RectTransform>(controller, "piecesLayer");
        placedPointer.position += RectTransformUtility.WorldToScreenPoint(camera, freeLayer.TransformPoint(freeDrop)) - RectTransformUtility.WorldToScreenPoint(camera, first.RectTransform.position);
        controller.Drag(first, placedPointer);
        Check("Unlocked group follows its grabbed point while dragging", (RectTransformUtility.WorldToScreenPoint(camera, first.RectTransform.TransformPoint(groupGrab)) - placedPointer.position).sqrMagnitude < 0.25f);
        var beforeFreeRelease = first.RectTransform.anchoredPosition;
        controller.EndDrag(first);
        Check("Unlocked fragment does not jump back after a free drop", (first.RectTransform.anchoredPosition - beforeFreeRelease).sqrMagnitude < 0.01f);
        placedPointer = PiecePointer(first, Vector2.zero);
        controller.BeginDrag(first, placedPointer);
        var layer = Get<RectTransform>(controller, "piecesLayer");
        var leftArea = Get<RectTransform>(controller, "leftPiecesArea");
        var sidePosition = (Vector2)layer.InverseTransformPoint(leftArea.TransformPoint(leftArea.rect.center));
        placedPointer.position = RectTransformUtility.WorldToScreenPoint(camera, layer.TransformPoint(sidePosition));
        controller.Drag(first, placedPointer);
        Check("Connected fragment follows its grip into the side area", (RectTransformUtility.WorldToScreenPoint(camera, first.RectTransform.position) - placedPointer.position).sqrMagnitude < 0.25f);
        controller.EndDrag(first);
        var expectedSpacing = second.CorrectAnchoredPosition - first.CorrectAnchoredPosition;
        Check("Connected fragment keeps full scale in the side area", first.RectTransform.localScale == Vector3.one && second.RectTransform.localScale == Vector3.one);
        Check("Connected fragment keeps its seams in the side area", ((second.RectTransform.anchoredPosition - first.RectTransform.anchoredPosition) - expectedSpacing).sqrMagnitude < 0.01f);
        controller.BeginHover(first);
        var enlargedScale = first.RectTransform.localScale.x;
        Check("Hover enlarges a connected fragment uniformly", enlargedScale > 1f && Mathf.Abs(second.RectTransform.localScale.x - enlargedScale) < 0.0001f && ((second.RectTransform.anchoredPosition - first.RectTransform.anchoredPosition) / enlargedScale - expectedSpacing).sqrMagnitude < 0.01f);
        controller.EndHover(first);
        Check("Pointer exit restores the fragment without deforming its seams", first.RectTransform.localScale == Vector3.one && second.RectTransform.localScale == Vector3.one && ((second.RectTransform.anchoredPosition - first.RectTransform.anchoredPosition) - expectedSpacing).sqrMagnitude < 0.01f);
        for (var i = 0; i < 5; i++) { controller.BeginHover(first); controller.EndHover(first); }
        Check("Repeated hover does not accumulate position drift", ((second.RectTransform.anchoredPosition - first.RectTransform.anchoredPosition) - expectedSpacing).sqrMagnitude < 0.01f);
        Check("Partially offscreen fragment remains accessible", GroupIntersectsCanvas(first.Group, layer));
        Capture("fragment-outside-frame.png");
        for (var i = 2; i < pieces.Count; i++) pieces[i].RectTransform.anchoredPosition = originalPositions[i];
        first.RectTransform.anchoredPosition = first.CorrectAnchoredPosition;
        second.RectTransform.anchoredPosition = second.CorrectAnchoredPosition;
        Invoke(controller, "ResolvePlacement", first.Group);
        settings.LockPlacedToggle.isOn = true;
        var third = pieces[2];
        third.RectTransform.anchoredPosition = third.CorrectAnchoredPosition + new Vector2(18f, 14f);
        Invoke(controller, "ResolvePlacement", first.Group);
        Check("Merging into placed fragments preserves their anchor", first.Group == third.Group && first.Group.IsPlaced && first.RectTransform.anchoredPosition == first.CorrectAnchoredPosition && third.RectTransform.anchoredPosition == third.CorrectAnchoredPosition);
        var bottom = pieces[4];
        bottom.RectTransform.anchoredPosition = bottom.CorrectAnchoredPosition;
        Invoke(controller, "ResolvePlacement", first.Group);
        settings.LockPlacedToggle.isOn = false;
        foreach (var grip in new[] { first, third, bottom })
        {
            foreach (var member in first.Group.Members) member.RectTransform.anchoredPosition = member.CorrectAnchoredPosition;
            Invoke(controller, "ResolvePlacement", first.Group);
            controller.BeginHover(grip);
            var tallPointer = PiecePointer(grip, new Vector2(12f, 8f));
            controller.BeginDrag(grip, tallPointer);
            var pointerStart = tallPointer.position;
            tallPointer.position = pointerStart + new Vector2(80f, 200f);
            controller.Drag(grip, tallPointer);
            Check("Tall fragment follows the cursor vertically beyond the screen edge", (RectTransformUtility.WorldToScreenPoint(camera, grip.RectTransform.TransformPoint(new Vector2(12f, 8f))) - tallPointer.position).sqrMagnitude < 0.25f);
            tallPointer.position = pointerStart + new Vector2(-100f, -200f);
            controller.Drag(grip, tallPointer);
            Check("Tall fragment follows the cursor back in both axes", (RectTransformUtility.WorldToScreenPoint(camera, grip.RectTransform.TransformPoint(new Vector2(12f, 8f))) - tallPointer.position).sqrMagnitude < 0.25f);
            var tallBeforeRelease = grip.RectTransform.anchoredPosition;
            controller.EndDrag(grip);
            Check("Partly visible tall fragment does not jump on release", (grip.RectTransform.anchoredPosition - tallBeforeRelease).sqrMagnitude < 0.01f);
        }
        Capture("tall-fragment-free-drag.png");
        var outsidePointer = PiecePointer(third, Vector2.zero);
        controller.BeginDrag(third, outsidePointer);
        outsidePointer.position += new Vector2(10000f, 10000f);
        controller.Drag(third, outsidePointer);
        controller.EndDrag(third);
        Check("Fully offscreen fragment is recovered without shrinking", GroupIntersectsCanvas(first.Group, layer) && first.RectTransform.localScale == Vector3.one);
        foreach (var member in first.Group.Members) member.RectTransform.anchoredPosition = member.CorrectAnchoredPosition;
        Invoke(controller, "ResolvePlacement", first.Group);
        settings.LockPlacedToggle.isOn = true;
        SetCaptureSize(800, 1000);
        CheckBoardAspect(0.5f);
        Check("Resizing preserves merged groups", first.Group == second.Group && (first.RectTransform.anchoredPosition - first.CorrectAnchoredPosition).sqrMagnitude < 0.25f && (second.RectTransform.anchoredPosition - second.CorrectAnchoredPosition).sqrMagnitude < 0.25f);
        Check("Resizing preserves placement lock state", first.Group.IsPlaced && controller.LockPlacedPieces && first.RectTransform.localScale == Vector3.one);
        Capture("portrait-window.png");
        Invoke(controller, "OpenSettings");
        var panel = settings.Panel;
        var root = (RectTransform)settings.Canvas.transform;
        Check("Settings fit narrow window", panel.rect.width * panel.localScale.x <= root.rect.width && panel.rect.height * panel.localScale.y <= root.rect.height);
        settings.PreviewToggle.isOn = false;
        Invoke(controller, "CloseSettings", false);
        SetCaptureSize(1920, 1080);
        foreach (var piece in pieces) piece.RectTransform.anchoredPosition = piece.CorrectAnchoredPosition;
        foreach (var piece in pieces) Invoke(controller, "ResolvePlacement", piece.Group);
        Invoke(controller, "EvaluateCompletion");
        Check("Puzzle completes after resizing", Get<bool>(controller, "puzzleCompleted"));
        Check("Completion reveals seamless photo", Get<RawImage>(controller, "boardGuideImage").color.a == 1f && !pieces[0].GetComponent<Image>().enabled);
        Check("Completion reveals photo even with preview disabled", !controller.PreviewEnabled && Get<RawImage>(controller, "boardGuideImage").color.a == 1f);
        Check("No-video completion remains a still", !Get<RawImage>(Get<PuzzleCompletionVideo>(controller, "completionVideo"), "overlay").enabled);

        var media = Get<PuzzleMediaEntry>(controller, "activeMedia").Copy();
        var videoPath = Path.GetFullPath("Tests/Fixtures/gallery-video.mp4");
        Check("Local validation video exists", File.Exists(videoPath));
        media.SetCustomVideoPath(videoPath);
        Set(media, "loopVideo", true);
        var completion = Get<PuzzleCompletionVideo>(controller, "completionVideo");
        Get<VideoPlayer>(completion, "videoPlayer").frameReady += RecordFirstFrame;
        playbackRequestedAt = EditorApplication.timeSinceStartup;
        firstFrameLabel = "Cold fixture";
        completion.Play(media);
    }

    private static void RecordFirstFrame(VideoPlayer player, long frame)
    {
        player.frameReady -= RecordFirstFrame;
        if (firstFrameLabel == "Warm fixture")
            Check("Prepared video preserves a short finished-photo moment", EditorApplication.timeSinceStartup - playbackRequestedAt >= 0.20);
        performance.Add($"{firstFrameLabel} first frame: {(EditorApplication.timeSinceStartup - playbackRequestedAt) * 1000:0.0} ms");
    }

    private static void MeasureGeneration()
    {
        foreach (var grid in new[] { 4, 8 })
        {
            Set(controller, "activeRows", grid);
            Set(controller, "activeColumns", grid);
            var timer = System.Diagnostics.Stopwatch.StartNew();
            Invoke(controller, "StartConfiguredPuzzle");
            timer.Stop();
            var textureMemory = 0L;
            foreach (var piece in Get<List<PuzzlePieceView>>(controller, "activePieces"))
                textureMemory += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(piece.PieceSprite.texture);
            performance.Add($"{grid}x{grid} generation: {timer.Elapsed.TotalMilliseconds:0.0} ms; piece texture memory: {textureMemory} bytes");
        }
        Set(controller, "activeRows", 4);
        Set(controller, "activeColumns", 4);
        Invoke(controller, "StartConfiguredPuzzle");
    }

    private static void BeginWarmVideoValidation()
    {
        var media = Get<PuzzleMediaEntry>(controller, "activeMedia").Copy();
        media.SetCustomVideoPath(Path.GetFullPath("Tests/Fixtures/gallery-video.mp4"));
        Set(controller, "activeMedia", media);
        Set(controller, "activeRows", 4);
        Set(controller, "activeColumns", 4);
        Invoke(controller, "StartConfiguredPuzzle");
        var pieces = Get<List<PuzzlePieceView>>(controller, "activePieces");
        for (var i = 0; i < 12; i++) pieces[i].RectTransform.anchoredPosition = pieces[i].CorrectAnchoredPosition;
        for (var i = 0; i < 12; i++) Invoke(controller, "ResolvePlacement", pieces[i].Group);
        Invoke(controller, "EvaluateCompletion");
        Check("Connecting 75 percent of pieces starts video preparation", !Get<bool>(controller, "puzzleCompleted") && Get<PuzzleMediaEntry>(Get<PuzzleCompletionVideo>(controller, "completionVideo"), "preparedMedia") != null);
    }

    private static void CheckLoosePieceEdgeDragging(List<PuzzlePieceView> pieces)
    {
        var layer = Get<RectTransform>(controller, "piecesLayer");
        var bounds = layer.rect;
        var inset = 6f / Get<Canvas>(controller, "gameCanvas").scaleFactor;
        var targets = new[]
        {
            new Vector2(bounds.xMin + inset, bounds.center.y),
            new Vector2(bounds.xMax - inset, bounds.center.y),
            new Vector2(bounds.center.x, bounds.yMin + inset),
            new Vector2(bounds.center.x, bounds.yMax - inset)
        };
        var directions = new[] { Vector2.left, Vector2.right, Vector2.down, Vector2.up };
        var labels = new[] { "left", "right", "bottom", "top" };
        var originalPositions = pieces.ConvertAll(piece => piece.RectTransform.anchoredPosition);
        var sample = pieces[0];
        var grabbed = Vector2.Scale(sample.RectTransform.rect.size, new Vector2(0.1f, 0.08f));
        try
        {
            // Keep neighboring pieces away from the boundary drops so these
            // checks exercise movement rather than an incidental connection.
            foreach (var piece in pieces) piece.RectTransform.anchoredPosition = bounds.center;
            for (var edge = 0; edge < targets.Length; edge++)
            {
                sample.RectTransform.anchoredPosition = bounds.center;
                var pointer = PiecePointer(sample, grabbed);
                controller.BeginDrag(sample, pointer);
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera, layer.TransformPoint(targets[edge]));
                controller.Drag(sample, pointer);
                var grabbedScreen = RectTransformUtility.WorldToScreenPoint(camera, sample.RectTransform.TransformPoint(grabbed));
                Check($"Single piece follows the pointer at the {labels[edge]} edge", (grabbedScreen - pointer.position).sqrMagnitude < 0.25f);
                var droppedPosition = sample.RectTransform.anchoredPosition;
                controller.EndDrag(sample);
                Check($"Partially visible single piece stays where dropped at the {labels[edge]} edge", (sample.RectTransform.anchoredPosition - droppedPosition).sqrMagnitude < 0.01f);

                pointer = PiecePointer(sample, grabbed);
                controller.BeginDrag(sample, pointer);
                var outside = targets[edge] + directions[edge] * (sample.RectTransform.rect.size.magnitude + 100f);
                pointer.position = RectTransformUtility.WorldToScreenPoint(camera, layer.TransformPoint(outside));
                controller.Drag(sample, pointer);
                grabbedScreen = RectTransformUtility.WorldToScreenPoint(camera, sample.RectTransform.TransformPoint(grabbed));
                Check($"Single piece can be dragged beyond the {labels[edge]} edge", (grabbedScreen - pointer.position).sqrMagnitude < 0.25f && !GroupIntersectsCanvas(sample.Group, layer));
                controller.EndDrag(sample);
                Check($"Fully offscreen single piece is reachable after a {labels[edge]} release", GroupIntersectsCanvas(sample.Group, layer) && sample.Group.Members.Count == 1);
            }
        }
        finally
        {
            Set(controller, "draggingGroup", null);
            Set(controller, "draggedPiece", null);
            for (var i = 0; i < pieces.Count; i++) pieces[i].RectTransform.anchoredPosition = originalPositions[i];
            sample.Group.SetHighlighted(false);
            Invoke(controller, "ApplyGroupRestingScale", sample.Group);
        }
    }

    private static bool GroupIntersectsCanvas(PuzzlePieceGroup group, RectTransform layer)
    {
        foreach (var member in group.Members)
        {
            var half = member.RectTransform.rect.size * member.RectTransform.localScale.x * 0.5f;
            var center = member.RectTransform.anchoredPosition;
            if (layer.rect.Overlaps(Rect.MinMaxRect(center.x - half.x, center.y - half.y, center.x + half.x, center.y + half.y))) return true;
        }
        return false;
    }

    private static PointerEventData PiecePointer(PuzzlePieceView piece, Vector2 local)
    {
        var canvas = Get<Canvas>(controller, "gameCanvas");
        return new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, piece.RectTransform.TransformPoint(local)),
            pointerPressRaycast = new RaycastResult { module = canvas.GetComponent<GraphicRaycaster>() }
        };
    }

    private static void ClickColorSurface(RectTransform surface, Vector2 normalized, Canvas canvas)
    {
        var local = surface.rect.min + Vector2.Scale(surface.rect.size, normalized);
        var data = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(camera, surface.TransformPoint(local)),
            pointerPressRaycast = new RaycastResult { module = canvas.GetComponent<GraphicRaycaster>() }
        };
        surface.GetComponent<PuzzleColorSurface>().OnPointerDown(data);
    }

    private static void CheckDefaultVideoEdges(RenderTexture target)
    {
        var previous = RenderTexture.active;
        var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            pixels.Apply();
            var edgeLight = new float[4];
            for (var i = 0; i < 16; i++)
            {
                var position = Mathf.Lerp(0.1f, 0.9f, i / 15f);
                var x = Mathf.RoundToInt(position * (target.width - 1));
                var y = Mathf.RoundToInt(position * (target.height - 1));
                edgeLight[0] += pixels.GetPixel(2, y).grayscale;
                edgeLight[1] += pixels.GetPixel(target.width - 3, y).grayscale;
                edgeLight[2] += pixels.GetPixel(x, 2).grayscale;
                edgeLight[3] += pixels.GetPixel(x, target.height - 3).grayscale;
            }
            var names = new[] { "left", "right", "bottom", "top" };
            for (var edge = 0; edge < names.Length; edge++)
                Check("Default video reaches the " + names[edge] + " photo edge without black padding", edgeLight[edge] / 16f > 0.01f);
        }
        finally
        {
            RenderTexture.active = previous;
            Object.Destroy(pixels);
        }
    }

    private static void CheckSpawnBounds()
    {
        var layer = Get<RectTransform>(controller, "piecesLayer");
        foreach (var piece in Get<List<PuzzlePieceView>>(controller, "activePieces"))
        {
            var half = piece.RectTransform.sizeDelta * piece.RectTransform.localScale.x * 0.5f;
            var position = piece.RectTransform.anchoredPosition;
            Check("Loose piece stays inside screen", layer.rect.Contains(position - half) && layer.rect.Contains(position + half));
        }
    }

    private static void CheckBoardAspect(float expected)
    {
        var board = Get<RectTransform>(controller, "boardGuideRect");
        Check("Photo aspect is preserved", Mathf.Abs(board.rect.width / board.rect.height - expected) < 0.001f);
        var frame = Get<PuzzleFrameView>(controller, "frameView").ContentRoot;
        Check("Board content remains positive", frame.rect.width > 0f && frame.rect.height > 0f);
    }

    private static void SetCaptureSize(int width, int height)
    {
        if (camera == null)
        {
            camera = new GameObject("Validation Camera").AddComponent<Camera>();
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.nearClipPlane = 0.1f; camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }
        camera.targetTexture = null;
        if (captureTarget != null) { captureTarget.Release(); Object.Destroy(captureTarget); }
        captureTarget = new RenderTexture(width, height, 24);
        captureTarget.Create();
        camera.targetTexture = captureTarget;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.scaleFactor = Mathf.Min(width / 1920f, height / 1080f);
        }
        Canvas.ForceUpdateCanvases();
        Invoke(controller, "RefreshResponsiveLayout");
    }

    private static void Capture(string filename)
    {
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = captureTarget });
        var previous = RenderTexture.active;
        RenderTexture.active = captureTarget;
        var texture = new Texture2D(captureTarget.width, captureTarget.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Output, filename), texture.EncodeToPNG());
        RenderTexture.active = previous;
        Object.Destroy(texture);
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) throw new Exception(name);
        checks.Add(name);
    }

    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, PrivateInstance).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, PrivateInstance).SetValue(target, value);
    private static object Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, PrivateInstance).Invoke(target, args);

    private static void Finish(bool passed, string message)
    {
        File.WriteAllLines(Path.Combine(Output, "performance-result.txt"), performance);
        EditorApplication.update -= Tick;
        Application.runInBackground = previousRunInBackground;
        File.WriteAllText(Path.Combine(Output, "validation-result.txt"), $"Passed: {passed}\nChecks: {checks.Count}\n{message}\n" + string.Join("\n", checks));
        if (passed) Debug.Log($"TileVerse gallery validation passed: {checks.Count} checks.");
        else Debug.LogError($"TileVerse gallery validation failed: {message}");
        SessionState.SetInt(SessionKey + ".result", passed ? 0 : 1);
        EditorApplication.isPlaying = false;
    }
}
#endif
