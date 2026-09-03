using System;
using System.Runtime.InteropServices;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

internal static class NativeImagePicker
{
    public static string OpenImageFile()
    {
#if UNITY_EDITOR
        return EditorUtility.OpenFilePanel("Выберите изображение", string.Empty, "png,jpg,jpeg");
#elif UNITY_STANDALONE_WIN
        return WindowsFileDialog.OpenFile();
#else
        return string.Empty;
#endif
    }
}

internal static class WindowsFileDialog
{
#if UNITY_STANDALONE_WIN
    [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class OpenFileName
    {
        public int structSize = Marshal.SizeOf(typeof(OpenFileName));
        public IntPtr dialogOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public string filter;
        public string customFilter;
        public int maxCustomFilter;
        public int filterIndex = 1;
        public StringBuilder file = new(260);
        public int maxFile = 260;
        public StringBuilder fileTitle = new(260);
        public int maxFileTitle = 260;
        public string initialDirectory = string.Empty;
        public string title = "Выберите изображение";
        public int flags = 0x00080000 | 0x00001000 | 0x00000800;
        public short fileOffset;
        public short fileExtension;
        public string defaultExtension = string.Empty;
        public IntPtr customData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public string templateName;
        public IntPtr reservedPointer = IntPtr.Zero;
        public int reservedInt;
        public int flagsEx;
    }

    public static string OpenFile()
    {
        return OpenFile("Image Files\0*.png;*.jpg;*.jpeg\0All Files\0*.*\0", "Выберите изображение");
    }

    public static string OpenVideoFile()
    {
        return OpenFile("Video Files\0*.mp4;*.mov;*.webm;*.m4v;*.avi\0All Files\0*.*\0", "Выберите видео");
    }

    private static string OpenFile(string filter, string title)
    {
        var dialog = new OpenFileName
        {
            filter = filter,
            initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            title = title
        };

        return GetOpenFileName(dialog) ? dialog.file.ToString() : string.Empty;
    }
#else
    public static string OpenFile()
    {
        return string.Empty;
    }
#endif
}
