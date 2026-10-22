using System.Collections.Generic;
using UnityEngine;

public enum PuzzleDisplayMode
{
    FullScreen,
    ExclusiveFullScreen,
    MaximizedWindow,
    Windowed
}

// Screen applies changes at the end of the frame. This setting is independent
// of the pending puzzle configuration, so it never resets the current assembly.
public sealed class PuzzleDisplaySettings
{
    public static IReadOnlyList<string> Labels { get; } = new[]
    {
        "Полный экран", "Эксклюзивный полный экран", "Оконный на весь экран", "Оконный"
    };

    private Vector2Int windowSize;
#if UNITY_EDITOR
    private PuzzleDisplayMode editorMode;
    public PuzzleDisplayMode CurrentMode => editorMode;
#else
    public PuzzleDisplayMode CurrentMode => FromUnityMode(Screen.fullScreenMode);
#endif

    public PuzzleDisplaySettings()
    {
        var mode = FromUnityMode(Screen.fullScreenMode);
#if UNITY_EDITOR
        editorMode = mode;
#endif
        var display = Screen.mainWindowDisplayInfo;
        windowSize = mode == PuzzleDisplayMode.Windowed
            ? new Vector2Int(Screen.width, Screen.height)
            : new Vector2Int(Mathf.Max(1, Mathf.Min(1280, display.width - 80)),
                Mathf.Max(1, Mathf.Min(720, display.height - 100)));
    }

    public void Apply(PuzzleDisplayMode mode)
    {
        if ((uint)mode >= Labels.Count || mode == CurrentMode) return;
#if UNITY_EDITOR
        // Do not resize or change the full-screen state of the Unity Editor.
        editorMode = mode;
#else
        if (CurrentMode == PuzzleDisplayMode.Windowed)
            windowSize = new Vector2Int(Screen.width, Screen.height);
        var display = Screen.mainWindowDisplayInfo;
        var size = mode == PuzzleDisplayMode.Windowed ? windowSize : new Vector2Int(display.width, display.height);
        Screen.SetResolution(Mathf.Max(1, size.x), Mathf.Max(1, size.y), ToUnityMode(mode));
#endif
    }

    public static FullScreenMode ToUnityMode(PuzzleDisplayMode mode) => mode switch
    {
        PuzzleDisplayMode.ExclusiveFullScreen => FullScreenMode.ExclusiveFullScreen,
        PuzzleDisplayMode.MaximizedWindow => FullScreenMode.MaximizedWindow,
        PuzzleDisplayMode.Windowed => FullScreenMode.Windowed,
        _ => FullScreenMode.FullScreenWindow
    };

    public static PuzzleDisplayMode FromUnityMode(FullScreenMode mode) => mode switch
    {
        FullScreenMode.ExclusiveFullScreen => PuzzleDisplayMode.ExclusiveFullScreen,
        FullScreenMode.MaximizedWindow => PuzzleDisplayMode.MaximizedWindow,
        FullScreenMode.Windowed => PuzzleDisplayMode.Windowed,
        _ => PuzzleDisplayMode.FullScreen
    };
}
