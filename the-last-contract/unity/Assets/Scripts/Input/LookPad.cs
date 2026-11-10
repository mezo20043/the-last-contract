using UnityEngine;
using UnityEngine.EventSystems;

namespace TLC.Player
{
    /// <summary>
    /// منطقة سحب الكاميرا — نص الشاشة اليمين. بتبلّغ إزاحة بالنقاط/ثانية.
    /// </summary>
    public class LookPad : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        [SerializeField] float sensitivity = 0.18f;
        [SerializeField] float maxDeltaPerFrame = 40f;

        public System.Action<Vector2> OnLook;      // إزاحة ملتوية (x=yaw, y=pitch)
        public bool IsDragging { get; private set; }

        public void OnBeginDrag(PointerEventData e) => IsDragging = true;
        public void OnEndDrag(PointerEventData e)   => IsDragging = false;

        public void OnDrag(PointerEventData e)
        {
            Vector2 d = e.delta;
            d.x = Mathf.Clamp(d.x, -maxDeltaPerFrame, maxDeltaPerFrame);
            d.y = Mathf.Clamp(d.y, -maxDeltaPerFrame, maxDeltaPerFrame);
            OnLook?.Invoke(d * sensitivity / Mathf.Max(0.01f, Time.deltaTime) * 0.016f);
        }
    }
}
