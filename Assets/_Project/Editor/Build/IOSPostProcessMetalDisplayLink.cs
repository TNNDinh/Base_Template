using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

public class IOSPostProcessMetalDisplayLink
{
    [PostProcessBuild(999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS) return;

        string folderPath = Path.Combine(buildPath, "Classes");
        if (!Directory.Exists(folderPath)) return;

        string[] files = Directory.GetFiles(folderPath, "*.mm", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string content = File.ReadAllText(file);
            if (content.Contains("shouldUseMetalDisplayLink"))
            {
                ReplaceReturnYesInMethod(file);
            }
        }
    }

    private static void ReplaceReturnYesInMethod(string file)
    {
        string content = File.ReadAllText(file);
        string methodName = "shouldUseMetalDisplayLink";
        int startPos = content.IndexOf(methodName);
        if (startPos == -1) return;

        int openBracePos = content.IndexOf('{', startPos);
        if (openBracePos == -1) return;

        int braces = 0;
        int closeBracePos = -1;
        for (int i = openBracePos; i < content.Length; i++)
        {
            if (content[i] == '{') braces++;
            else if (content[i] == '}')
            {
                braces--;
                if (braces == 0)
                {
                    closeBracePos = i;
                    break;
                }
            }
        }

        if (closeBracePos != -1)
        {
            string methodContent = content.Substring(openBracePos, closeBracePos - openBracePos + 1);
            string newMethodContent = methodContent.Replace("return YES;", "return NO;");
            
            if (methodContent != newMethodContent)
            {
                content = content.Remove(openBracePos, closeBracePos - openBracePos + 1);
                content = content.Insert(openBracePos, newMethodContent);
                File.WriteAllText(file, content);
                Debug.Log($"[PostProcessBuild] Replaced 'return YES;' with 'return NO;' in '{methodName}' in file: {file}");
            }
        }
    }
}
