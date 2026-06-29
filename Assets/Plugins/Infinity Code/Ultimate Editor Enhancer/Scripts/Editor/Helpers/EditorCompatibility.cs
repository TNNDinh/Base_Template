/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace InfinityCode.UltimateEditorEnhancer
{
    public class EditorCompatibility
    {
        public static Object EntityIdToObject(int id)
        {
#if UNITY_6000_3_OR_NEWER
            return EditorUtility.EntityIdToObject(Compatibility.InstanceToEntity(id));
#else
            return EditorUtility.InstanceIDToObject(id);
#endif
        }

        public static string GetAssetPath(int instanceId)
        {
#if UNITY_6000_3_OR_NEWER
            return AssetDatabase.GetAssetPath(Compatibility.InstanceToEntity(instanceId));
#else
            return AssetDatabase.GetAssetPath(instanceId);
#endif
        }

        public static int GetSelectionInstanceId()
        {
#if UNITY_6000_3_OR_NEWER
            return Compatibility.EntityToInstance(Selection.activeEntityId);
#else
            return Selection.activeInstanceID;
#endif
        }

        public static int[] GetSelectionInstanceIds()
        {
#if UNITY_6000_3_OR_NEWER
            return Selection.entityIds.Select(Compatibility.EntityToInstance).ToArray();
#else
            return Selection.instanceIDs;
#endif
        }

        public static bool IsLoadingAssetPreview(int id)
        {
#if UNITY_6000_3_OR_NEWER
            return AssetPreview.IsLoadingAssetPreview(Compatibility.InstanceToEntity(id));
#else
            return AssetPreview.IsLoadingAssetPreview(id);
#endif
        }

        public static void PingObject(int id)
        {
#if UNITY_6000_3_OR_NEWER
            EditorGUIUtility.PingObject(Compatibility.InstanceToEntity(id));
#else
            EditorGUIUtility.PingObject(id);
#endif
        }

        public static void SetSelectionInstanceIds(int[] ids)
        {
#if UNITY_6000_3_OR_NEWER
            Selection.entityIds = ids.Select(Compatibility.InstanceToEntity).ToArray();
#else
            Selection.instanceIDs = ids;
#endif
        }
    }
}