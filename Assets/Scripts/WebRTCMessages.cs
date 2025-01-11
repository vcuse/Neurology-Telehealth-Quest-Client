using System;
using TMPro;
using UnityEngine;

public class WebRTCMessages : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logTextBox;
    [SerializeField] private bool printMessages;
    private string startText = "WebRTC Messages:\n";

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (printMessages)
        {
            if (!logTextBox.text.Contains(startText)) {
                logTextBox.text += startText;
            }
            logTextBox.text += logString + Environment.NewLine;
        }
    }
}
