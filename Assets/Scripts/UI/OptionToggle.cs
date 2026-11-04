using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>A button in a one-of-many group that shows whether it is the current choice.</summary>
    [RequireComponent(typeof(Button))]
    public class OptionToggle : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private Color normalColor = new Color(0.16f, 0.2f, 0.26f, 0.95f);
        [SerializeField] private Color selectedColor = new Color(0.85f, 0.64f, 0.25f, 1f);
        [SerializeField] private Color normalTextColor = new Color(0.9f, 0.9f, 0.88f);
        [SerializeField] private Color selectedTextColor = new Color(0.08f, 0.08f, 0.1f);

        public Button Button => GetComponent<Button>();
        public Text Label => label;

        public void SetSelected(bool selected)
        {
            if (background != null) background.color = selected ? selectedColor : normalColor;
            if (label != null) label.color = selected ? selectedTextColor : normalTextColor;
        }
    }
}
