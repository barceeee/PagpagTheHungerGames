using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.Localization
{
    /// <summary>Static UI label whose text comes from the string table.</summary>
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool upperCase;

        public string Key
        {
            get => key;
            set { key = value; Refresh(); }
        }

        private void OnEnable() => Refresh();

        public void Refresh()
        {
            if (string.IsNullOrEmpty(key)) return;
            var value = Loc.Get(key);
            GetComponent<Text>().text = upperCase ? value.ToUpperInvariant() : value;
        }
    }
}
