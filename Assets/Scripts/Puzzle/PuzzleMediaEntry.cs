using System;
using UnityEngine;
using UnityEngine.Video;

[Serializable]
public sealed class PuzzleMediaEntry
{
    [SerializeField] private string displayName = "Картина";
    [SerializeField] private Texture2D image;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private string customVideoPath;
    [SerializeField] private bool loopVideo;
    [SerializeField] private PuzzleFrameStyle frameStyle = PuzzleFrameStyle.Default;

    [NonSerialized] public bool IsRuntimeImage;

    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Картина" : displayName;
    public Texture2D Image => image;
    public VideoClip VideoClip => videoClip;
    public string CustomVideoPath => customVideoPath;
    public bool LoopVideo => loopVideo;
    public PuzzleFrameStyle FrameStyle => frameStyle;
    public bool HasVideo => videoClip != null || !string.IsNullOrWhiteSpace(customVideoPath);

    public PuzzleMediaEntry()
    {
    }

    public PuzzleMediaEntry(string name, Texture2D sourceImage)
    {
        displayName = name;
        image = sourceImage;
    }

    public PuzzleMediaEntry Copy()
    {
        return new PuzzleMediaEntry
        {
            displayName = displayName,
            image = image,
            videoClip = videoClip,
            customVideoPath = customVideoPath,
            loopVideo = loopVideo,
            frameStyle = frameStyle,
            IsRuntimeImage = IsRuntimeImage
        };
    }

    public void SetImage(string name, Texture2D sourceImage, bool isRuntimeImage)
    {
        displayName = name;
        image = sourceImage;
        IsRuntimeImage = isRuntimeImage;
    }

    public void SetCustomVideoPath(string path)
    {
        videoClip = null;
        customVideoPath = path;
    }
}
