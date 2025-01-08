using System;
using TMPro;
using UnityEngine;

public class ConnectionMessages : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logTextBox;

    private void Start()
    {
        Debug.Log("Started up logging.");
    }

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
        logTextBox.text += logString + Environment.NewLine;
    }
}
