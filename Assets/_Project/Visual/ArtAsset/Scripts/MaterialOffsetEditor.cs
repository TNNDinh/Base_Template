#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class MaterialOffsetEditor : EditorWindow
{
    private string folderPath = "Assets/Materials"; // Đường dẫn mặc định tới folder chứa material
    private readonly Dictionary<Material, bool> materialSelection = new(); // Lưu trạng thái chọn của từng material
    private Vector2 scrollPosition; // Vị trí cuộn cho danh sách material
    private bool selectAll; // Trạng thái của checkbox "Select All"
    private float xValue = 1.0f; // Giá trị X mặc định

    private void OnGUI()
    {
        GUILayout.Label("Material Offset Editor", EditorStyles.boldLabel);

        // Field để nhập đường dẫn folder
        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);
        if (GUILayout.Button("Load Materials")) LoadMaterials();

        // Checkbox "Select All"
        var newSelectAll = EditorGUILayout.Toggle("Select All", selectAll);
        if (newSelectAll != selectAll)
        {
            selectAll = newSelectAll;
            // Sao chép khóa vào mảng tạm để tránh lỗi khi thay đổi collection
            var keys = new Material[materialSelection.Keys.Count];
            materialSelection.Keys.CopyTo(keys, 0);
            foreach (var material in keys) materialSelection[material] = selectAll;
        }

        // Hiển thị số lượng material đang được chọn
        var selectedCount = materialSelection.Values.Count(v => v);
        GUILayout.Label($"Đã chọn: {selectedCount}/{materialSelection.Count} material");

        // Hiển thị danh sách material với checkbox
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Sao chép các khóa vào một mảng tạm thời
        var toggleKeys = new Material[materialSelection.Keys.Count];
        materialSelection.Keys.CopyTo(toggleKeys, 0);

        // Tạo một dictionary tạm thời để lưu các thay đổi
        var changes = new Dictionary<Material, bool>();

        // Lặp qua mảng tạm thời
        foreach (var material in toggleKeys)
        {
            var currentValue = materialSelection[material];
            var newValue = EditorGUILayout.Toggle(material.name, currentValue);
            if (newValue != currentValue) changes[material] = newValue;
        }

        // Áp dụng các thay đổi sau khi lặp
        foreach (var change in changes) materialSelection[change.Key] = change.Value;

        EditorGUILayout.EndScrollView();

        // Field để nhập số X
        xValue = EditorGUILayout.FloatField("X Value", xValue);

        // Button Execute
        if (GUILayout.Button("Execute")) ExecuteOffsetUpdate();
    }

    [MenuItem("Tools/Material Offset Editor")]
    public static void ShowWindow()
    {
        GetWindow<MaterialOffsetEditor>("Material Offset Editor");
    }

    private void LoadMaterials()
    {
        materialSelection.Clear();
        var guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) materialSelection.Add(material, false);
        }

        if (materialSelection.Count == 0)
            Debug.LogWarning(
                "Không tìm thấy material nào trong folder hoặc không có material nào dùng shader Universal Render Pipeline/Unlit.");
    }

    private void ExecuteOffsetUpdate()
    {
        if (xValue == 0)
        {
            Debug.LogError("Giá trị X không thể bằng 0.");
            return;
        }

        foreach (var kvp in materialSelection)
            if (kvp.Value) // Nếu material được chọn
            {
                var material = kvp.Key;
                var materialName = material.name;

                var numberPart = Regex.Match(materialName, @"\d+").Value;
                if (!string.IsNullOrEmpty(numberPart))
                {
                    var number = int.Parse(numberPart);
                    var newOffsetX = number / xValue;

                    Vector2 currentOffset = material.GetVector("_BaseMap_ST");
                    material.SetVector("_BaseMap_ST", new Vector4(currentOffset.x, currentOffset.y, newOffsetX, 0));
                    Debug.Log($"Đã cập nhật Offset X của {material.name} thành {newOffsetX}");
                }
                else
                {
                    Debug.LogWarning($"Không tìm thấy số trong tên material: {materialName}");
                }
            }

        AssetDatabase.SaveAssets(); // Lưu thay đổi
    }
}
#endif