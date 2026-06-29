using Ezg.Feature.Shared.GameData;
#if UNITY_EDITOR
using System.Drawing.Printing;
using Ezg.Feature.Shared.GameData;
using System.IO;
using Ezg.Package.CsvReader;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Ezg.Core.Extensions
{
    public class EditorExtensions : EditorWindow
    {
        #region Extensions
#if UNITY_EDITOR
        [MenuItem("CustomEditor/BlackFace/Delete Prefs & ReCompilation %#x")]
        private static void ReCompilation()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("CustomEditor/BlackFace/ReCompile & Play %#e")]
        private static void ReCompileAndPlay()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
            }

            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CompilationPipeline.RequestScriptCompilation();
        }


        private static void OnCompilationFinished(object context)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            EditorApplication.EnterPlaymode();
        }

        private static void OnCompilationFinishedGenData(object context)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinishedGenData;
            GenDataManager.GenFullProcess();
        }

        [MenuItem("CustomEditor/BlackFace/Reload all data CSV %h")]
        private static void DownloadAllData()
        {
            CsvImportManager.ImportAllData();
            AssetDatabase.Refresh();
            CompilationPipeline.compilationFinished += OnCompilationFinishedGenData;
            CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("CustomEditor/BlackFace/Force Reload all data CSV %#h")]
        private static void ForceDownloadAllData()
        {
            CsvImportManager.ImportAllData(true);
            AssetDatabase.Refresh();
            CompilationPipeline.compilationFinished += OnCompilationFinishedGenData;
            CompilationPipeline.RequestScriptCompilation();
        }

        [MenuItem("CustomEditor/BlackFace/Find Asset By GUID")]
        public static void FindAssetByGUID()
        {
            string guid = "0e6c572223aa9854190ce41ba21575e6";
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                Debug.Log("Found asset at path: " + path);
            }
            else
            {
                Debug.LogError("Asset with GUID not found. It might have been deleted.");
            }
        }
#endif

        #endregion

        #region Scenes

        [MenuItem("Scene/SplashScene", false, 0)]
        public static void SplashScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/SplashScene.unity");
        }

        [MenuItem("Scene/HomeScene", false, 1)]
        public static void HomeScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/HomeScene.unity");
        }

        [MenuItem("Scene/BattleScene", false, 2)]
        public static void BattleScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/BattleScene.unity");
        }

        [MenuItem("Scene/TestScene", false, 3)]
        public static void TestScene()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/TestScene.unity");
        }

        [MenuItem("Scene/Recorder", false, 4)]
        public static void RecorderScene()
        {
            EditorSceneManager.OpenScene("Assets/AlphaRecorder/RecorderScene.unity");
        }

        [MenuItem("GameObject/Anchor Snap/Parent And Children (CTRL+[)%[", false, 0)]
        private static void SnapParentAndChildrenGameObject()
        {
            SnapAnchorsMultiple(Selection.activeGameObject);
        }

        [MenuItem("GameObject/Anchor Snap/Just Parent %q", false, 0)]
        private static void SnapParentGameObject()
        {
            SnapAnchors(Selection.activeGameObject);
        }

        private static void SnapAnchors(GameObject gameObject)
        {
            RectTransform recTransform = null;
            RectTransform parentTransform = null;

            if (gameObject.transform.parent != null)
            {
                if (gameObject.GetComponent<RectTransform>() != null)
                {
                    recTransform = gameObject.GetComponent<RectTransform>();
                }
                else
                {
                    return;
                }

                if (parentTransform == null)
                {
                    parentTransform = gameObject.transform.parent.GetComponent<RectTransform>();
                }

                Undo.RecordObject(recTransform, "Snap Anchors");

                Vector2 offsetMin = recTransform.offsetMin;
                Vector2 offsetMax = recTransform.offsetMax;
                Vector2 anchorMin = recTransform.anchorMin;
                Vector2 anchorMax = recTransform.anchorMax;
                Vector2 parent_scale = new Vector2(parentTransform.rect.width, parentTransform.rect.height);
                recTransform.anchorMin = new Vector2(anchorMin.x + (offsetMin.x / parent_scale.x),
                    anchorMin.y + (offsetMin.y / parent_scale.y));
                recTransform.anchorMax = new Vector2(anchorMax.x + (offsetMax.x / parent_scale.x),
                    anchorMax.y + (offsetMax.y / parent_scale.y));
                recTransform.offsetMin = Vector2.zero;
                recTransform.offsetMax = Vector2.zero;
            }
        }

        private static void SnapAnchorsMultiple(GameObject gameObject)
        {
            SnapAnchors(gameObject);
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                SnapAnchorsMultiple(gameObject.transform.GetChild(i).gameObject);
            }
        }

        #endregion
    }
}
#endif