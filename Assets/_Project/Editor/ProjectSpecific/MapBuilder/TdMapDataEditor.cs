using Ezg.Feature.MapBuilder;
using UnityEditor;
using UnityEngine;

namespace Ezg.Feature.MapBuilder.EditorTools
{
    /// <summary>
    /// Inspector của <see cref="TdMapData"/>: hiện tóm tắt map và nút mở Map Builder.
    /// </summary>
    [CustomEditor(typeof(TdMapData))]
    public class TdMapDataEditor : UnityEditor.Editor
    {
        #region Public

        public override void OnInspectorGUI()
        {
            var map = (TdMapData)target;

            if (GUILayout.Button("Mở Map Builder", GUILayout.Height(30f)))
            {
                TdMapBuilderWindow.OpenWith(map);
            }

            EditorGUILayout.Space(6f);
            DrawSummary(map);
            EditorGUILayout.Space(6f);

            DrawDefaultInspector();
        }

        #endregion

        #region Private

        private void DrawSummary(TdMapData map)
        {
            TdMapValidation validation = TdMapPath.Validate(map);

            EditorGUILayout.LabelField("Tóm tắt", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Lưới: {map.Width} x {map.Height} ô, cạnh {map.CellSize}");
            EditorGUILayout.LabelField($"World: {map.WorldSize.x} x {map.WorldSize.y}");
            EditorGUILayout.LabelField($"Spawn {map.Spawn}  →  Goal {map.Goal}");

            EditorGUILayout.HelpBox(validation.Message, validation.IsValid ? MessageType.Info : MessageType.Error);
        }

        #endregion
    }
}
