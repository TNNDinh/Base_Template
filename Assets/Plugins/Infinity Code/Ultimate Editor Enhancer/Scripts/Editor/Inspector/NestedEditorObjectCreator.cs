/*           INFINITY CODE          */
/*     https://infinity-code.com    */

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace InfinityCode.UltimateEditorEnhancer.InspectorTools
{
    public static class NestedEditorObjectCreator
    {
        private static GUIContent createMaterialContent;
        private static GUIContent createScriptableObjectContent;

        public static void DrawButton(ref Rect area, SerializedProperty property)
        {
            bool isMaterial = IsMaterialField(property);
            Type soType = isMaterial ? null : GetScriptableObjectType(property);

            if (!isMaterial && soType == null) return;

            Rect buttonRect = area;
            buttonRect.x = EditorGUIUtility.labelWidth;
            buttonRect.width = 20;
            area.xMax -= buttonRect.width + 2;

            if (isMaterial) DrawMaterialButton(buttonRect, property);
            else DrawScriptableObjectButton(buttonRect, property, soType);
        }

        private static void DrawMaterialButton(Rect buttonRect, SerializedProperty property)
        {
            if (createMaterialContent == null) createMaterialContent = new GUIContent("+", "Create and assign new Material");
            if (!GUI.Button(buttonRect, createMaterialContent, EditorStyles.miniButton)) return;

            string defaultName = property.displayName != null ? property.displayName : "New Material";
            ScheduleAssetCreation(
                property,
                "Create Material",
                "Create Material",
                defaultName,
                "mat",
                "Select location for new Material",
                () => new Material(RenderPipelineHelper.GetDefaultShader())
            );
        }

        private static void DrawScriptableObjectButton(Rect buttonRect, SerializedProperty property, Type soType)
        {
            string typeName = soType.Name;
            if (createScriptableObjectContent == null) createScriptableObjectContent = new GUIContent("+");
            createScriptableObjectContent.tooltip = "Create and assign new " + typeName;
            if (!GUI.Button(buttonRect, createScriptableObjectContent, EditorStyles.miniButton)) return;

            string defaultName = property.displayName ?? "New " + typeName;
            ScheduleAssetCreation(
                property,
                "Create " + typeName,
                "Create " + typeName,
                defaultName,
                "asset",
                "Select location for new " + typeName,
                () => ScriptableObject.CreateInstance(soType)
            );
        }

        private static void ScheduleAssetCreation(SerializedProperty property, string undoName, string title, string defaultName, string extension, string message, Func<Object> createAsset)
        {
            Object targetObject = property.serializedObject.targetObject;
            string propertyPath = property.propertyPath;

            EditorApplication.delayCall += () =>
            {
                if (!targetObject) return;

                string path = EditorUtility.SaveFilePanelInProject(title, defaultName, extension, message);
                if (string.IsNullOrEmpty(path)) return;

                SerializedObject serializedObject = new SerializedObject(targetObject);
                SerializedProperty targetProperty = serializedObject.FindProperty(propertyPath);
                if (targetProperty == null) return;

                Object asset = createAsset();
                AssetDatabase.CreateAsset(asset, path);
                Undo.RegisterCreatedObjectUndo(asset, undoName);

                Undo.RecordObject(targetObject, undoName);
                targetProperty.objectReferenceValue = asset;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(targetObject);

                EditorGUIUtility.PingObject(asset);
            };
        }

        private static Type GetScriptableObjectType(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) return null;

            string typeName = property.type;
            if (!typeName.StartsWith("PPtr<$")) return null;
            typeName = typeName.Substring(6, typeName.Length - 7);

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
            {
                if (type.Name == typeName && !type.IsAbstract) return type;
            }

            return null;
        }

        private static bool IsMaterialField(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference) return false;
            if (property.objectReferenceValue is Material) return true;

            return property.type == "PPtr<$Material>";
        }
    }
}
