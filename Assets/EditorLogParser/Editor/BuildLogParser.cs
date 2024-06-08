using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class BuildLogParser : EditorWindow
{
    [MenuItem("Tools/Parse Build Log")]
    public static void ParseBuildLog()
    {
        string logFilePath = GetLogFilePath();
        string tempLogFilePath = Path.Combine(Application.temporaryCachePath, "Editor_temp.log");

        try
        {
            File.Copy(logFilePath, tempLogFilePath, true);

            List<(string fileName, string size, string percentage)> fileSizes = ParseEditorLog(tempLogFilePath);
            ShowParsedData(fileSizes);
        }
        catch (IOException e)
        {
            Debug.LogError($"Failed to access Editor.log file: {e.Message}");
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
                    fileSizes.Add((fileName, size, percentage));
                }
            }
            else if (line.Contains("Used Assets and files from the Resources folder, sorted by uncompressed size:"))
            {
                inAssetsSection = true;
            }
        }

        return fileSizes;
    }

    private static void ShowParsedData(List<(string fileName, string size, string percentage)> fileSizes)
    {
        var window = GetWindow<BuildLogParser>("Parsed Build Log Data");
        window.fileSizes = fileSizes;
        window.Show();
    }

    private List<(string fileName, string size, string percentage)> fileSizes;

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        if (fileSizes == null || fileSizes.Count == 0)
        {
            EditorGUILayout.LabelField("No data to display.");
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Width(position.width), GUILayout.Height(position.height));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("File Name", EditorStyles.boldLabel, GUILayout.Width(position.width / 2));
        EditorGUILayout.LabelField("Size", EditorStyles.boldLabel, GUILayout.Width(position.width / 4));
        EditorGUILayout.LabelField("Percentage", EditorStyles.boldLabel, GUILayout.Width(position.width / 4));
        EditorGUILayout.EndHorizontal();

        foreach (var (fileName, size, percentage) in fileSizes)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fileName, GUILayout.Width(position.width / 2));
            EditorGUILayout.LabelField(size, GUILayout.Width(position.width / 4));
            EditorGUILayout.LabelField(percentage, GUILayout.Width(position.width / 4));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }
}