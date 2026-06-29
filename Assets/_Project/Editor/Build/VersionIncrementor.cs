#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

[InitializeOnLoad]
class VersionIncrementor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public static readonly int maxNumberPerPhase = 999;

    [MenuItem("Tools/Build/Increase Both Versions &v")]
    static void IncreaseBothVersions()
    {
        IncreaseBuild();
        IncreasePlatformVersion();
    }

    [MenuItem("Tools/Build/Increase Current Build Version")]
    static void IncreaseBuild()
    {
        IncrementVersion(new[] { 0, 0, 1 });
    }

    [MenuItem("Tools/Build/Increase Minor Version")]
    static void IncreaseMinor()
    {
        IncrementVersion(new[] { 0, 1, 0 });
    }

    [MenuItem("Tools/Build/Increase Major Version")]
    static void IncreaseMajor()
    {
        IncrementVersion(new[] { 1, 0, 0 });
    }

    [MenuItem("Tools/Build/Increase Platform Version")]
    static void IncreasePlatformVersion()
    {
        PlayerSettings.Android.bundleVersionCode += 1;
        PlayerSettings.iOS.buildNumber = (int.Parse(PlayerSettings.iOS.buildNumber) + 1).ToString();
    }

    static void IncrementVersion(int[] version)
    {
        string[] lines = PlayerSettings.bundleVersion.Split('.');

        for (int i = lines.Length - 1; i >= 0; i--)
        {
            bool isNumber = int.TryParse(lines[i], out int numberValue);

            if (isNumber && version.Length - 1 >= i)
            {
                if (i > 0 && version[i] + numberValue > maxNumberPerPhase)
                {
                    version[i - 1]++;

                    version[i] = 0;
                }
                else
                {
                    version[i] += numberValue;
                }
            }
        }

        PlayerSettings.bundleVersion = $"{version[0]}.{version[1]}.{version[2]}";
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        PlayerSettings.Android.keystorePass = "deviloper2025";
        PlayerSettings.Android.keyaliasPass = "deviloper2025";

        // bool shouldIncrement = EditorUtility.DisplayDialog(
        //     "Increment Version?",
        //     $"Current: {PlayerSettings.bundleVersion}",
        //     "Yes",
        //     "No"
        // );
        //
        // if (shouldIncrement) IncreaseBothVersions();
        IncreaseBothVersions();
    }
}

#endif