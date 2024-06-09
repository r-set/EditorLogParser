using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class BuildLogParser : EditorWindow
{
    private static bool showInternalFiles = false;
    private static List<(string fileName, string size, string percentage)> cachedFileSizes;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Parse Build Log")]
    public static void ShowWindow()
    {
        GetWindow<BuildLogParser>("Parsed Build Log Data");
    }

    private void OnGUI()
    {
        showInternalFiles = EditorGUILayout.Toggle("Show Internal Files", showInternalFiles);

        if (GUILayout.Button("Parse Build Log"))
        {
            ParseBuildLog();
        }

        if (cachedFileSizes == null || cachedFileSizes.Count == 0)
        {
            EditorGUILayout.LabelField("No data to display.");
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File Name", EditorStyles.boldLabel, GUILayout.Width(position.width / 3));
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel, GUILayout.Width(position.width / 6));
        EditorGUILayout.LabelField("Percentage", EditorStyles.boldLabel, GUILayout.Width(position.width / 6));
        EditorGUILayout.LabelField("Action", EditorStyles.boldLabel, GUILayout.Width(position.width / 3));
        EditorGUILayout.EndHorizontal();

        foreach (var (fileName, size, percentage) in cachedFileSizes)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fileName, GUILayout.Width(position.width / 3));
            EditorGUILayout.LabelField(size, GUILayout.Width(position.width / 6));
            EditorGUILayout.LabelField(percentage, GUILayout.Width(position.width / 6));

            if (!IsInternalFile(fileName))
            {
                if (GUILayout.Button("Show in Project", GUILayout.Width(position.width / 6)))
                {
                    ShowFileInProject(fileName);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private static bool IsInternalFile(string fileName)
    {
        return fileName.StartsWith("Built-in ") || fileName.Contains("unity_builtin_extra") || fileName.Contains("Packages");
    }

    public static void ParseBuildLog()
    {
        string logFilePath = GetLogFilePath();
        string tempLogFilePath = Path.Combine(Application.temporaryCachePath, "Editor_temp.log");

        try
        {
            File.Copy(logFilePath, tempLogFilePath, true);

            cachedFileSizes = ParseEditorLog(tempLogFilePath);
            if (cachedFileSizes == null || cachedFileSizes.Count == 0)
            {
                Debug.LogWarning("No relevant file sizes found in the log.");
            }
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to access Editor.log file: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"An unexpected error occurred: {e.Message}");
        }
        finally
        {
            if (File.Exists(tempLogFilePath))
            {
                File.Delete(tempLogFilePath);
            }
        }
    }

    private static string GetLogFilePath()
    {
        string logFilePath = string.Empty;

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            string userName = Environment.UserName;
            logFilePath = $"C:/Users/{userName}/AppData/Local/Unity/Editor/Editor.log";
        }
        else if (Application.platform == RuntimePlatform.OSXEditor)
        {
            logFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/Library/Logs/Unity/Editor.log";
        }
        else if (Application.platform == RuntimePlatform.LinuxEditor)
        {
            logFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/.config/unity3d/Editor.log";
        }

        return logFilePath;
    }

    private static List<(string fileName, string size, string percentage)> ParseEditorLog(string filePath)
    {
        var sizePattern = new Regex(@"\s+(\d+(?:\.\d+)?\s*(?:kb|mb))\s+(\d+(\.\d+)?%)\s+(.+)");
        var fileSizes = new List<(string fileName, string size, string percentage)>();

        var inAssetsSection = false;

        foreach (var line in File.ReadLines(filePath))
        {
            if (inAssetsSection)
            {
                if (line.Contains("-------------------------------------------------------------------------------"))
                    break;

                var match = sizePattern.Match(line);
                if (match.Success)
                {
                    var size = match.Groups[1].Value;
                    var percentage = match.Groups[2].Value;
                    var fileName = match.Groups[4].Value;

                    if (showInternalFiles || !IsInternalFile(fileName))
                    {
                        fileSizes.Add((fileName, size, percentage));
                    }
                }
            }
            else if (line.Contains("Used Assets and files from the Resources folder, sorted by uncompressed size:"))
            {
                inAssetsSection = true;
            }
        }

        return fileSizes;
    }

    private void ShowFileInProject(string fileName)
    {
        string assetPath = GetAssetPath(fileName);
        UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }
        else
        {
            Debug.LogWarning($"Asset not found: {assetPath}");
        }
    }

    private string GetAssetPath(string fileName)
    {
        if (fileName.StartsWith("Assets/"))
        {
            return fileName;
        }
        return "Assets/" + fileName;
    }
}
