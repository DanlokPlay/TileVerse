using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public sealed class PuzzleCompletionVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private RawImage overlay;
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private Text statusText;
    [SerializeField] private float fadeDuration = 0.45f;

    private RenderTexture runtimeTexture;
    private Coroutine fadeRoutine;

    public void Configure(
        VideoPlayer player,
        AudioSource source,
        RawImage image,
        CanvasGroup group,
        Text status)
    {
        videoPlayer = player;
        audioSource = source;
        overlay = image;
        overlayGroup = group;
        statusText = status;
        ResetPresentation();
    }

    private void Awake()
    {
        ResetPresentation();
    }

    private void OnDestroy()
    {
        StopPlayback();
        if (runtimeTexture != null)
        {
            Destroy(runtimeTexture);
        }
    }

    public void Play(PuzzleMediaEntry entry)
    {
        StopPlayback();
        ResetPresentation();

        if (entry == null || !entry.HasVideo)
        {
            ShowStatus("Видео для этой картины ещё не выбрано.");
            return;
        }

        if (videoPlayer == null || overlay == null)
        {
            ShowStatus("В сцене не настроен видеоплеер.");
            return;
        }

        ConfigureRenderTarget();
        videoPlayer.isLooping = entry.LoopVideo;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, audioSource != null);
        if (audioSource != null)
        {
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        videoPlayer.prepareCompleted += HandlePrepared;
        videoPlayer.errorReceived += HandleError;
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

        videoPlayer.Prepare();
    }

    public void StopPlayback()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= HandlePrepared;
            videoPlayer.errorReceived -= HandleError;
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
            }
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
    }

    private void ConfigureRenderTarget()
    {
        var width = Mathf.Max(256, Screen.width);
        var height = Mathf.Max(256, Screen.height);
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
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = runtimeTexture;
        overlay.texture = runtimeTexture;
    }

    private void HandlePrepared(VideoPlayer player)
    {
        player.Play();
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
        StopPlayback();
        ResetPresentation();
        ShowStatus($"Не удалось запустить видео: {message}");
    }

    private IEnumerator FadeIn()
    {
        var duration = Mathf.Max(0.01f, fadeDuration);
        for (var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
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
    }

    private void ShowStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.gameObject.SetActive(true);
    }
}
