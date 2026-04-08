using UnityEngine;
using TPS.Runtime.Time;
using TPS.Runtime.Weather;

namespace TPS.Runtime.Debugging
{
    /// <summary>
    /// Displays world time and weather state in the top-left corner using OnGUI.
    /// Attach to CoreServices alongside WorldClock and WeatherSystem.
    /// </summary>
    public sealed class DebugWorldHUD : MonoBehaviour
    {
        private GUIStyle _labelStyle;
        private GUIStyle _boxStyle;

        private void OnGUI()
        {
            EnsureStyles();

            float x = 10f;
            float y = 10f;
            float w = 280f;
            float lineH = 24f;

            GUI.Box(new Rect(x - 4, y - 4, w, lineH * 2 + 14), "", _boxStyle);

            string timeText = "Time: --";
            if (WorldClock.Instance != null)
            {
                timeText = $"Time: {WorldClock.Instance.GetFormattedTime()}";
            }
            GUI.Label(new Rect(x, y, w, lineH), timeText, _labelStyle);

            string weatherText = "Weather: --";
            if (WeatherSystem.Instance != null)
            {
                weatherText = $"Weather: {WeatherSystem.Instance.CurrentWeather}";
            }
            GUI.Label(new Rect(x, y + lineH + 2, w, lineH), weatherText, _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
            _labelStyle.normal.textColor = Color.white;

            _boxStyle = new GUIStyle(GUI.skin.box);
        }
    }
}
