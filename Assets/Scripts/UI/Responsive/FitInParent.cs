using UnityEngine;

namespace Pagpag.UI
{
    /// <summary>
    /// Lays out a region designed at a fixed size so it is never clipped: it scales down uniformly when the
    /// space around it is smaller than the design, and (optionally) stretches to use extra space when larger.
    /// The rect must have its anchors at a single point (anchorMin == anchorMax).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class FitInParent : MonoBehaviour
    {
        [Tooltip("Size the region was designed at, in reference pixels.")]
        public Vector2 designSize = new Vector2(1920, 1080);
        [Tooltip("Shrink to fit the parent's width / height.")]
        public bool fitWidth = true;
        public bool fitHeight = true;
        [Tooltip("Use all the width / height available (anchored children spread out on big screens).")]
        public bool stretchWidth;
        public bool stretchHeight;
        [Tooltip("Space to leave free in the parent (total, both sides), e.g. for neighbouring regions.")]
        public Vector2 reserved;
        [Tooltip("Never scale above this.")]
        public float maxScale = 1f;

        private Vector2 _lastParent;

        public float Scale { get; private set; } = 1f;

        private void OnEnable()
        {
            _lastParent = Vector2.zero;
            Apply();
        }

        private void LateUpdate()
        {
            var parent = transform.parent as RectTransform;
            if (parent != null && parent.rect.size != _lastParent) Apply();
        }

        public void Apply()
        {
            var parent = transform.parent as RectTransform;
            if (parent == null) return;
            var p = parent.rect.size;
            _lastParent = p;
            if (p.x <= 1f || p.y <= 1f) return;
            var avail = new Vector2(Mathf.Max(1f, p.x - reserved.x), Mathf.Max(1f, p.y - reserved.y));

            var s = maxScale;
            if (fitWidth) s = Mathf.Min(s, avail.x / designSize.x);
            if (fitHeight) s = Mathf.Min(s, avail.y / designSize.y);
            s = Mathf.Max(0.05f, s);
            Scale = s;

            var rt = (RectTransform)transform;
            rt.localScale = new Vector3(s, s, 1f);
            rt.sizeDelta = new Vector2(
                stretchWidth ? Mathf.Max(designSize.x, avail.x / s) : designSize.x,
                stretchHeight ? Mathf.Max(designSize.y, avail.y / s) : designSize.y);
        }

        /// <summary>Height actually taken on screen, in the parent's units.</summary>
        public float ScaledHeight => ((RectTransform)transform).rect.height * transform.localScale.y;
    }
}
