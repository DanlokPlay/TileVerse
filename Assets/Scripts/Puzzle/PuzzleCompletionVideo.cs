using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class PuzzleCompletionVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private RawImage overlay;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject statusPanel;
    [SerializeField, Min(0f)] private float completedPhotoDuration = 0.25f;
    [SerializeField] private float fadeDuration = 0.45f;
    [SerializeField, Min(1f)] private float prepareTimeout = 15f;

    private RenderTexture runtimeTexture;
    private Coroutine fadeRoutine;
    private Coroutine loadRoutine;
    private Coroutine startRoutine;
    private bool paused;
    private bool resumePlayback;
    private bool playRequested;
    private bool preparationFailed;
    private bool hasFirstFrame;
    private PuzzleMediaEntry preparedMedia;

    public void Configure(
        VideoPlayer player,
        AudioSource source,
        RawImage image,
        CanvasGroup group,
        TMP_Text status,
        GameObject messagePanel = null)
    {
        videoPlayer = player;
        audioSource = source;
        overlay = image;
        overlayGroup = group;
        statusText = status;
        statusPanel = messagePanel;
        ResetPresentation();
    }

    private void Awake()
    {
        ResetPresentation();
    }

    private void OnDestroy() => StopPlayback();

    public void Play(PuzzleMediaEntry entry)
    {
        ResetPresentation();

        if (entry == null || !entry.HasVideo)
        {
            StopPlayback();
            ShowStatus("Видео для этой картины ещё не выбрано.");
            return;
        }

        if (videoPlayer == null || overlay == null)
        {
            StopPlayback();
            ShowStatus("В сцене не настроен видеоплеер.");
            return;
        }

        if (entry.VideoClip == null && !IsUsableVideoPath(entry.CustomVideoPath))
        {
            StopPlayback();
            ShowStatus("Видео недоступно. Выберите другое в меню Esc.");
            return;
        }

        if (playRequested || !MatchesPreparedMedia(entry)) BeginPreparation(entry);
        playRequested = true;
        if (preparationFailed) { ShowStatus("Видео недоступно. Выберите другое в меню Esc."); return; }
        videoPlayer.sendFrameReadyEvents = true;
        ConfigureRenderTarget();
        if (videoPlayer.isPrepared) StartPreparedPlayback();
    }

    // Prepare once near completion, without decoding a playing stream or audio.
    public void Prepare(PuzzleMediaEntry entry)
    {
        if (entry == null || !entry.HasVideo || videoPlayer == null || overlay == null || MatchesPreparedMedia(entry)) return;
        if (entry.VideoClip == null && !IsUsableVideoPath(entry.CustomVideoPath)) return;
        BeginPreparation(entry);
    }

    private bool MatchesPreparedMedia(PuzzleMediaEntry entry) => preparedMedia != null &&
        preparedMedia.VideoClip == entry.VideoClip && preparedMedia.LoopVideo == entry.LoopVideo &&
        string.Equals(preparedMedia.CustomVideoPath, entry.CustomVideoPath, StringComparison.Ordinal);

    private void BeginPreparation(PuzzleMediaEntry entry)
    {
        StopPlayback();
        ResetPresentation();
        preparedMedia = entry.Copy();
        ConfigureRenderTarget();
        videoPlayer.isLooping = entry.LoopVideo;
        // Fill the photo opening uniformly; encoded side bars fall outside it.
        videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.sendFrameReadyEvents = false;

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.errorReceived += HandleError;
        videoPlayer.frameReady += HandleFirstFrame;
        if (entry.VideoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = entry.VideoClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = entry.CustomVideoPath;
        }

        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        loadRoutine = StartCoroutine(WaitForFirstFrame());
        try { videoPlayer.Prepare(); }
        catch (Exception exception) { HandleError(videoPlayer, exception.Message); }
    }

    public void StopPlayback()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.errorReceived -= HandleError;
            videoPlayer.frameReady -= HandleFirstFrame;
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
            videoPlayer.clip = null;
            videoPlayer.url = string.Empty;
        }
        if (overlay != null) { overlay.texture = null; overlay.enabled = false; }
        if (runtimeTexture != null)
        {
            runtimeTexture.Release();
            Destroy(runtimeTexture);
            runtimeTexture = null;
        }
        paused = false;
        resumePlayback = false;
        playRequested = false;
        hasFirstFrame = false;
        preparationFailed = false;
        preparedMedia = null;
    }

    public void SetPaused(bool value)
    {
        if (paused == value || videoPlayer == null) return;
        paused = value;
        if (paused)
        {
            resumePlayback = videoPlayer.isPlaying;
            if (resumePlayback) videoPlayer.Pause();
        }
        else if (resumePlayback)
        {
            resumePlayback = false;
            if (hasFirstFrame) videoPlayer.Play();
            else StartPreparedPlayback();
        }
    }

    public void ResetPresentation()
    {
        if (overlayGroup != null)
        {
            overlayGroup.alpha = 0f;
            overlayGroup.blocksRaycasts = false;
            overlayGroup.interactable = false;
        }

        if (overlay != null)
        {
            overlay.enabled = false;
        }

        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
        if (statusPanel != null) statusPanel.SetActive(false);
    }

    private void ConfigureRenderTarget()
    {
        var size = overlay.rectTransform.rect.size;
        var canvas = overlay.canvas;
        var scale = canvas != null ? canvas.scaleFactor : 1f;
        var limit = Mathf.Min(1f, 2048f / Mathf.Max(1f, size.x * scale, size.y * scale));
        var width = Mathf.Max(1, Mathf.RoundToInt(size.x * scale * limit));
        var height = Mathf.Max(1, Mathf.RoundToInt(size.y * scale * limit));
        if (runtimeTexture == null || runtimeTexture.width != width || runtimeTexture.height != height)
        {
            if (runtimeTexture != null)
            {
                runtimeTexture.Release();
                Destroy(runtimeTexture);
            }

            runtimeTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "PuzzleCompletionVideo"
            };
            runtimeTexture.Create();
            var previous = RenderTexture.active;
            RenderTexture.active = runtimeTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = runtimeTexture;
        overlay.texture = runtimeTexture;
    }

    private void HandlePrepared(VideoPlayer player)
    {
        player.prepareCompleted -= HandlePrepared;
        if (player.audioTrackCount > 0)
        {
            player.EnableAudioTrack(0, audioSource != null);
            if (audioSource != null) player.SetTargetAudioSource(0, audioSource);
        }
        if (loadRoutine != null) { StopCoroutine(loadRoutine); loadRoutine = null; }
        if (playRequested) StartPreparedPlayback();
    }

    private void StartPreparedPlayback()
    {
        if (paused) { resumePlayback = true; return; }
        if (startRoutine != null) return;
        startRoutine = StartCoroutine(ShowCompletedPhotoThenPlay());
    }

    private IEnumerator ShowCompletedPhotoThenPlay()
    {
        for (var elapsed = 0f; elapsed < completedPhotoDuration;)
        {
            if (!paused) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        while (paused) yield return null;
        startRoutine = null;
        if (loadRoutine == null) loadRoutine = StartCoroutine(WaitForFirstFrame());
        videoPlayer.Play();
    }

    private void HandleFirstFrame(VideoPlayer player, long frame)
    {
        if (!playRequested) return;
        hasFirstFrame = true;
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
        player.frameReady -= HandleFirstFrame;
        player.sendFrameReadyEvents = false;
        if (overlay != null)
        {
            overlay.enabled = true;
        }

        if (overlayGroup != null)
        {
            fadeRoutine = StartCoroutine(FadeIn());
        }
    }

    private void HandleError(VideoPlayer player, string message)
    {
        var showError = playRequested;
        var failedMedia = preparedMedia;
        StopPlayback();
        preparedMedia = failedMedia;
        preparationFailed = true;
        ResetPresentation();
        Debug.LogWarning($"TileVerse: video playback failed: {message}");
        if (showError) ShowStatus("Видео недоступно. Выберите другое в меню Esc.");
    }

    private static bool IsUsableVideoPath(string path)
    {
        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && !uri.IsFile) return true;
        try
        {
            var file = new FileInfo(uri != null && uri.IsFile ? uri.LocalPath : path);
            return file.Exists && file.Length > 0;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException)
        {
            return false;
        }
    }

    private IEnumerator WaitForFirstFrame()
    {
        var elapsed = 0f;
        while (elapsed < prepareTimeout && !hasFirstFrame && (playRequested || !videoPlayer.isPrepared))
        {
            if (!paused) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        loadRoutine = null;
        if (!videoPlayer.isPrepared || (playRequested && !hasFirstFrame)) HandleError(videoPlayer, "No first frame received before timeout.");
    }

    private IEnumerator FadeIn()
    {
        var duration = Mathf.Max(0.01f, fadeDuration);
        for (var elapsed = 0f; elapsed < duration;)
        {
            if (paused) { yield return null; continue; }
            elapsed += Time.unscaledDeltaTime;
            if (overlayGroup != null)
            {
                overlayGroup.alpha = Mathf.Clamp01(elapsed / duration);
            }

            yield return null;
        }

        if (overlayGroup != null)
        {
            overlayGroup.alpha = 1f;
        }
        fadeRoutine = null;
    }

    private void ShowStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.gameObject.SetActive(true);
        if (statusPanel != null) statusPanel.SetActive(true);
    }
}
