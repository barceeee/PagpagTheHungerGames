using System.Collections.Generic;
using System.Text;
using Pagpag.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>Shows the latest feedback messages (newest first). Warnings are highlighted.</summary>
    public class MessageLog : MonoBehaviour
    {
        [SerializeField] private Text text;
        [SerializeField] private int maxLines = 4;

        private readonly List<(string text, bool important)> _lines = new List<(string, bool)>();

        private void OnEnable() => GameEvents.MessagePosted += Add;
        private void OnDisable() => GameEvents.MessagePosted -= Add;

        public void Add(string message, bool important)
        {
            if (string.IsNullOrEmpty(message)) return;
            _lines.Insert(0, (message, important));
            if (_lines.Count > maxLines) _lines.RemoveAt(_lines.Count - 1);
            Render();
        }

        private void Render()
        {
            var sb = new StringBuilder();
            for (var i = 0; i < _lines.Count; i++)
            {
                var (msg, important) = _lines[i];
                var alpha = i == 0 ? "FF" : i == 1 ? "BB" : "88";
                var color = important ? "FF7A6E" : "EDE7DA";
                if (i > 0) sb.Append('\n');
                sb.Append($"<color=#{color}{alpha}>{(i == 0 ? "<b>" : "")}{Escape(msg)}{(i == 0 ? "</b>" : "")}</color>");
            }
            text.text = sb.ToString();
        }

        private static string Escape(string s) => s.Replace("<", "‹").Replace(">", "›");
    }
}
