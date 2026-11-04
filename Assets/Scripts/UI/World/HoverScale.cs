using UnityEngine;
using UnityEngine.EventSystems;

namespace Pagpag.UI
{
    /// <summary>Grows an interactable element slightly while the pointer is over it.</summary>
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.08f;
        [SerializeField] private float speed = 14f;

        private float _target = 1f;

        public void OnPointerEnter(PointerEventData eventData) => _target = hoverScale;
        public void OnPointerExit(PointerEventData eventData) => _target = 1f;

        private void OnDisable()
        {
            _target = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            var s = Mathf.Lerp(transform.localScale.x, _target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
