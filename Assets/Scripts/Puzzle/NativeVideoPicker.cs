using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

internal static class NativeVideoPicker
{
    public static string OpenVideoFile()
    {
#if UNITY_EDITOR
        return EditorUtility.OpenFilePanel("Выберите видео", string.Empty, "mp4,mov,webm,m4v,avi");
#elif UNITY_STANDALONE_WIN
        return WindowsFileDialog.OpenVideoFile();
#else
        return string.Empty;
#endif
    }
}
