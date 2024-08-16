using ExtensionFunctions;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class Logger : MonoBehaviour
    {
        [SerializeField] private GameObject logTextPrefab;
        [SerializeField] private ushort logLineLength = 30;

        // Print the log message to the screen and save it to the log file
        public void Log(string message, Color? color = null, bool alsoInConsole = true)
        {
            for (var i = 0; i < message.Length / logLineLength + 1; i++)
                Instantiate(logTextPrefab, transform).GetComponent<TextMeshProUGUI>().Apply(it =>
                {
                    it.text =
                        message.Substring(i * logLineLength, math.min(message.Length, (i + 1) * logLineLength));
                    it.color = color ?? Color.white;
                });
            if (alsoInConsole) Debug.Log(message);
        }
    }
}