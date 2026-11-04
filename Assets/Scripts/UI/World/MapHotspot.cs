using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>A clickable location label on the world map.</summary>
    [RequireComponent(typeof(Button))]
    public class MapHotspot : MonoBehaviour
    {
        [SerializeField] private string locationId;
        [SerializeField] private Image marker;
        [SerializeField] private Text nameText;
        [SerializeField] private Text statusText;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private GameObject hereRing;

        public string LocationId => locationId;
        public Button Button => GetComponent<Button>();

        public void SetState(string status, Color markerColor, bool locked, bool selected, bool here)
        {
            marker.color = markerColor;
            statusText.text = status;
            statusText.color = markerColor;
            nameText.color = locked ? new Color(0.7f, 0.7f, 0.7f) : Color.white;
            selectedFrame.SetActive(selected);
            hereRing.SetActive(here);
        }
    }
}
