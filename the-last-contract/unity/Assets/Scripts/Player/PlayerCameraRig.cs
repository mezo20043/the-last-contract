using UnityEngine;
using Unity.Cinemachine;

namespace TLC.Player
{
    /// <summary>
    /// كاميرا آدم: تقليدية من ورا الكتف مع سحب للإطار.
    /// بتستخدم CinemachineCamera مع جسم يتحكم في الدوران.
    /// </summary>
    public class PlayerCameraRig : MonoBehaviour
    {
        [Header("مراجع")]
        [SerializeField] LookPad lookPad;
        [SerializeField] CinemachineCamera vcam;
        [SerializeField] Transform pivot;

        [Header("حدود")]
        [SerializeField] float sensitivity = 1.0f;
        [SerializeField] float minPitch = -35f;
        [SerializeField] float maxPitch = 55f;
        [SerializeField] float followSmooth = 8f;

        float _yaw;
        float _pitch;

        void OnEnable()  { if (lookPad) lookPad.OnLook += HandleLook; }
        void OnDisable() { if (lookPad) lookPad.OnLook -= HandleLook; }

        void LateUpdate()
        {
            if (pivot == null) return;

            // اتبع اللاعب بسلاسة
            pivot.position = Vector3.Lerp(pivot.position, transform.position, followSmooth * Time.deltaTime);
            pivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        void HandleLook(Vector2 delta)
        {
            _yaw   += delta.x * sensitivity;
            _pitch -= delta.y * sensitivity;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        /// <summary>أوقات السينمائية اليدوية — صفّر الدوران.</summary>
        public void SnapTo(float yaw, float pitch)
        {
            _yaw = yaw; _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }
    }
}
