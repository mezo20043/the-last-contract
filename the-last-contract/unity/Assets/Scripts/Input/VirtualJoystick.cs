using UnityEngine;
using UnityEngine.EventSystems;

namespace TLC.Player
{
    /// <summary>
    /// عصاية تحكم افتراضية — بتشتغل بـ UI Pointer Events، من غير أي اعتماد على Input System.
    /// تربطها بصورة دائرية في الـ Canvas.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField] float handleRange = 70f;
        [SerializeField] float deadZone = 0.12f;

        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }

        void Start()
        {
            if (background == null) background = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData e)
        {
            IsHeld = true;
            OnDrag(e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background, e.position, e.pressEventCamera, out var local))
                return;

            // تطبيع بالنسبة لنصف قطر الخلفية
            Vector2 half = background.rect.size * 0.5f;
            Vector2 norm = new Vector2(
                Mathf.Clamp(local.x / half.x, -1f, 1f),
                Mathf.Clamp(local.y / half.y, -1f, 1f));

            if (norm.magnitude < deadZone) norm = Vector2.zero;

            Value = norm;

            if (handle) handle.anchoredPosition = norm * handleRange;
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsHeld = false;
            Value = Vector2.zero;
            if (handle) handle.anchoredPosition = Vector2.zero;
        }
    }
}
