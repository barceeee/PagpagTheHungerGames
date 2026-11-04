using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>One row in the sidebar's location list.</summary>
    [RequireComponent(typeof(Button))]
    public class LocationListItem : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private Image background;
        [SerializeField] private Image dot;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;

        private static readonly Color Normal = new Color(0.1f, 0.13f, 0.17f, 1f);
        private static readonly Color Selected = new Color(0.2f, 0.3f, 0.45f, 1f);

        public string LocationId => locationId;
        public Button Button => GetComponent<Button>();

        public void SetState(string displayName, string status, Color dotColor, bool locked, bool selected)
        {
            nameText.text = locked ? displayName + "  (Locked)" : displayName;
            nameText.color = locked ? new Color(0.55f, 0.57f, 0.6f) : new Color(0.93f, 0.92f, 0.88f);
            statusText.text = status;
            statusText.color = dotColor;
            dot.color = dotColor;
            background.color = selected ? Selected : Normal;
        }
    }
}
