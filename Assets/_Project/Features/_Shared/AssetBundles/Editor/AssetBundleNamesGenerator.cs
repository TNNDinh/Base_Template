using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class AssetBundleNamesGenerator
    {
        private const string AssetRefsPath =
            "Assets/_Project/Features/Systems/AssetBundles/AssetRefs.cs";

        [MenuItem("Tools/AssetBundle/Generate AssetRefs C#")]
        public static void GenerateAssetRefs()
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Auto-generated — do not edit manually.");
            sb.AppendLine("// Regenerate: Tools > AssetBundle > Generate AssetRefs C#");
            sb.AppendLine("// or click \"Generate C# AssetRefs\" in AssetBundleConfig inspector.");
            sb.AppendLine("namespace Ezg.Feature.Shared");
            sb.AppendLine("{");
            sb.AppendLine("    public static class AssetRefs");
            sb.AppendLine("    {");

            WriteTypeMembers(sb, typeof(AssetBundleName), "AssetBundleName", 2);

            sb.AppendLine("    }");
            sb.AppendLine("}");

            WriteIfChanged(AssetRefsPath, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[AssetBundleNamesGenerator] Generated: {AssetRefsPath}");
        }

        private static void WriteTypeMembers(StringBuilder sb, Type type, string bundleNamePath, int indent)
        {
            var pad = new string(' ', indent * 4);

            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (f.FieldType != typeof(string)) continue;
                sb.AppendLine(
                    $"{pad}public static AssetRef {f.Name}(string path) => new AssetRef(path, {bundleNamePath}.{f.Name});");
            }

            foreach (var nested in type.GetNestedTypes(BindingFlags.Public))
            {
                sb.AppendLine();
                sb.AppendLine($"{pad}public static class {nested.Name}");
                sb.AppendLine($"{pad}{{");
                WriteTypeMembers(sb, nested, $"{bundleNamePath}.{nested.Name}", indent + 1);
                sb.AppendLine($"{pad}}}");
            }
        }

        private static void WriteIfChanged(string assetPath, string content)
        {
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(fullPath) && File.ReadAllText(fullPath) == content) return;
            File.WriteAllText(fullPath, content);
            AssetDatabase.ImportAsset(assetPath);
        }
    }
}