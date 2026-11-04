using UnityEngine;

namespace Pagpag.UI
{
    /// <summary>Gentle breathing motion for character previews and portraits.</summary>
    public class IdleBob : MonoBehaviour
    {
        [SerializeField] private float amplitude = 6f;
        [SerializeField] private float speed = 1.6f;
        [SerializeField] private float squash = 0.012f;

        private RectTransform _rect;
        private Vector2 _origin;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _origin = _rect.anchoredPosition;
        }

        private void OnEnable() => _origin = ((RectTransform)transform).anchoredPosition;

        private void Update()
        {
            var s = Mathf.Sin(Time.unscaledTime * speed * Mathf.PI);
            _rect.anchoredPosition = _origin + new Vector2(0f, s * amplitude);
            _rect.localScale = new Vector3(1f - s * squash, 1f + s * squash, 1f);
        }
    }
}
