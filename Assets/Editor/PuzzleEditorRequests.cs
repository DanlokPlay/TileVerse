#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
internal static class PuzzleEditorRequests
{
    private static readonly string Root = Directory.GetParent(Application.dataPath).FullName;
    private static readonly string Request = Path.Combine(Root, "Temp/TileVerseGallery.request");
    private static double nextCheck;

    static PuzzleEditorRequests() => EditorApplication.update += Check;

    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        var command = File.ReadAllText(Request).Trim();
        if (command.StartsWith("rebuild") && !PuzzleSceneInstaller.PrepareResources()) return;
        File.Delete(Request);
        Directory.CreateDirectory(Path.Combine(Root, "Logs"));
        var status = Path.Combine(Root, "Logs/clean-editor-request.txt");
        try
        {
            if (command.StartsWith("rebuild")) PuzzleSceneInstaller.InstallCurrentScene();
            File.WriteAllText(status, "Scene rebuilt successfully. " + DateTime.UtcNow.ToString("O"));
            if (command.EndsWith("validate")) PuzzleGalleryValidation.RunAutomated();
        }
        catch (Exception error)
        {
            File.WriteAllText(status, error.ToString());
            Debug.LogException(error);
        }
    }
}
#endif
