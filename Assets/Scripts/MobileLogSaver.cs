using System.IO;
using UnityEngine;

public class MobileLogSaver : MonoBehaviour
{
    private string logPath;

    void Awake()
    {
        logPath = Path.Combine("/sdcard/Documents/", "unity_log.txt");
        // start with a fresh file each launch
        File.WriteAllText(logPath, $"--- Log started {System.DateTime.Now} ---{System.Environment.NewLine}");
        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string line = $"{System.DateTime.Now:HH:mm:ss} [{type}] {logString}";
        if (type == LogType.Exception || type == LogType.Error)
            line += $"{System.Environment.NewLine}{stackTrace}";
        File.AppendAllText(logPath, line + System.Environment.NewLine);
    }

    // Optional: a UI button to open the file location
    public void OpenLogFolder()
    {
        // on Android this will open the folder in a file‑manager app
        // (requires Unity 2022.2+; otherwise just tell the user the path)
        Debug.Log($"Log file: {logPath}");
    }
}