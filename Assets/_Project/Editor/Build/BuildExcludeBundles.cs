using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BuildExcludeBundles : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    private const string TempFolderName = "TempExcludedBundles";
    private static string TempFolderPath => Path.Combine(Directory.GetCurrentDirectory(), TempFolderName);
    private static string StreamingAssetsPath => Path.Combine(Application.dataPath, "StreamingAssets");

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // Only run for Android builds (or all if preferred, but user mentioned AAB)
        if (report.summary.platform != BuildTarget.Android) return;

        if (!Directory.Exists(StreamingAssetsPath)) return;

        string[] bundleFiles = Directory.GetFiles(StreamingAssetsPath, "*.bundle", SearchOption.AllDirectories);
        if (bundleFiles.Length == 0) return;

        if (!Directory.Exists(TempFolderPath))
        {
            Directory.CreateDirectory(TempFolderPath);
        }

        Debug.Log($"[BuildExcludeBundles] Moving {bundleFiles.Length} .bundle files to temporary folder to exclude from build.");

        foreach (string file in bundleFiles)
        {
            MoveFileToTemp(file);
            // Also move the .meta file if it exists
            string metaFile = file + ".meta";
            if (File.Exists(metaFile))
            {
                MoveFileToTemp(metaFile);
            }
        }

        AssetDatabase.Refresh();
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!Directory.Exists(TempFolderPath)) return;

        string[] tempFiles = Directory.GetFiles(TempFolderPath, "*", SearchOption.AllDirectories);
        if (tempFiles.Length == 0)
        {
            Directory.Delete(TempFolderPath, true);
            return;
        }

        Debug.Log($"[BuildExcludeBundles] Restoring {tempFiles.Length / 2} .bundle files (including metas) back to StreamingAssets.");

        foreach (string tempFile in tempFiles)
        {
            RestoreFileFromTemp(tempFile);
        }

        Directory.Delete(TempFolderPath, true);
        AssetDatabase.Refresh();
    }

    private void MoveFileToTemp(string filePath)
    {
        string relativePath = filePath.Replace(StreamingAssetsPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string destPath = Path.Combine(TempFolderPath, relativePath);

        string destDir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        if (File.Exists(destPath))
        {
            File.Delete(destPath);
        }

        File.Move(filePath, destPath);
    }

    private void RestoreFileFromTemp(string tempFilePath)
    {
        string relativePath = tempFilePath.Replace(TempFolderPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string destPath = Path.Combine(StreamingAssetsPath, relativePath);

        string destDir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }

        if (File.Exists(destPath))
        {
            File.Delete(destPath);
        }

        File.Move(tempFilePath, destPath);
    }
}
