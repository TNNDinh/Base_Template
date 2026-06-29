/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.IO;
using InfinityCode.UltimateEditorEnhancer.Windows;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer.ProjectTools
{
    [InitializeOnLoad]
    public static class ProjectCreateAnimatorController
    {
        static ProjectCreateAnimatorController()
        {
            ProjectItemDrawer.Listener listener = ProjectItemDrawer.Register(nameof(ProjectCreateAnimatorController), DrawButton, ProjectToolOrder.CreateAnimatorController);
            listener.folderName = "Animations";
        }

        private static void CreateAnimatorController(ProjectItem item)
        {
            Selection.activeObject = item.asset;

            string filename = "New Animator Controller";
            string path = item.path + $"/{filename}.controller";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
#if UNITY_6000_3_OR_NEWER
                Compatibility.InstanceToEntity(0),
#else
                0,
#endif
                ScriptableObject.CreateInstance<EndNameEdit>(),
                path,
                EditorIconContents.animatorController.image as Texture2D,
                null);

        }

        private static void DrawButton(ProjectItem item)
        {
            if (!Prefs.projectCreateAnimatorController) return;

            Rect r = item.rect;
            r.xMin = r.xMax - 18;
            r.height = 16;

            item.rect.xMax -= 18;

            if (GUI.Button(r, TempContent.Get(EditorIconContents.animatorController.image, "Create Animator Controller"), GUIStyle.none))
            {
                CreateAnimatorController(item);
            }
        }

        private class EndNameEdit :
#if UNITY_6000_4_OR_NEWER            
            AssetCreationEndAction
#else
            EndNameEditAction
#endif
        {
#if UNITY_6000_4_OR_NEWER
            public override void Action(EntityId instanceId, string pathName, string resourceFile)
#else
            public override void Action(int instanceId, string pathName, string resourceFile)
#endif
            {
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(pathName);
                ProjectWindowUtil.ShowCreatedAsset(controller);
            }
        }
    }
}