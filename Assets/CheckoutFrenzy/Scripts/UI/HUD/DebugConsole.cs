using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CryingSnow.CheckoutFrenzy.UI
{
    public class DebugConsole : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI logText;
        [SerializeField] private int maxLogs = 10;

        private Queue<string> logQueue = new Queue<string>();

        private void Start()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            logQueue.Enqueue($"[{type}] {logString}");
            if (logQueue.Count > maxLogs) logQueue.Dequeue();
            logText.text = string.Join("\n", logQueue);
        }
    }
}
