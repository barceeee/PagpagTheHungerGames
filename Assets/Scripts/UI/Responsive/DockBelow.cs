using UnityEngine;

namespace Pagpag.UI
{
    /// <summary>
    /// Keeps a region just below another one whose height can change with screen size (the HUD).
    /// <see cref="Mode.TopEdge"/> moves a stretched region's top edge; <see cref="Mode.Position"/> moves a
    /// top-anchored element.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class DockBelow : MonoBehaviour
    {
        public enum Mode { TopEdge, Position }

        public RectTransform target;
        public Mode mode = Mode.TopEdge;
        public float gap;

        private float _last = -1f;

        private void LateUpdate() => Apply();

        public void Apply()
        {
            if (target == null) return;
            var h = target.rect.height * target.localScale.y + gap;
            if (Mathf.Approximately(h, _last)) return;
            _last = h;
            var rt = (RectTransform)transform;
            if (mode == Mode.TopEdge) rt.offsetMax = new Vector2(rt.offsetMax.x, -h);
            else rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -h);
        }
    }
}
