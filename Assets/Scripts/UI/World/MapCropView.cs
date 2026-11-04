using UnityEngine;
using UnityEngine.UI;

namespace Pagpag.UI
{
    /// <summary>
    /// Shows a zoomed-in region of the map art without distortion. Used for location
    /// backgrounds and the sidebar thumbnail, so each place gets its own view of the district.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MapCropView : MonoBehaviour
    {
        private Vector2 _mapSize = Vector2.one;
        private Vector2 _center;
        private float _widthFraction = 1f;
        private bool _hasView;

        public RawImage Image => GetComponent<RawImage>();

        /// <param name="centerPx">Centre in map pixels, origin top-left.</param>
        /// <param name="widthFraction">How much of the map width to show (0–1).</param>
        public void Show(Texture texture, Vector2 mapSize, Vector2 centerPx, float widthFraction)
        {
            Image.texture = texture;
            _mapSize = mapSize;
            _center = centerPx;
            _widthFraction = Mathf.Clamp01(widthFraction);
            _hasView = true;
            Apply();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_hasView) Apply();
        }

        private void Apply()
        {
            var rect = ((RectTransform)transform).rect;
            var aspect = rect.height > 1f ? rect.width / rect.height : 16f / 9f;
            var w = _widthFraction;
            var h = w * _mapSize.x / aspect / _mapSize.y;
            if (h > 1f)
            {
                w /= h;
                h = 1f;
            }
            var cx = _center.x / _mapSize.x;
            var cy = 1f - _center.y / _mapSize.y;
            var x = Mathf.Clamp(cx - w / 2f, 0f, 1f - w);
            var y = Mathf.Clamp(cy - h / 2f, 0f, 1f - h);
            Image.uvRect = new Rect(x, y, w, h);
        }
    }
}
