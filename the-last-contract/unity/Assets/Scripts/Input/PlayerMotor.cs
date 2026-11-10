using UnityEngine;

namespace TLC.Player
{
    /// <summary>
    /// حركة آدم: مشي / جري / دوران سلس + جاذبية. مبنية على CharacterController.
    /// الكاميرا مش بتتكفل بالدوران — الـ PlayerMotor بياخد اتجاه الكاميرا من Transform.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("مراجع")]
        [SerializeField] VirtualJoystick moveStick;
        [SerializeField] Transform cameraTransform;

        [Header("سرعات")]
        [SerializeField] float walkSpeed = 1.6f;      // آدم بيمشي هادي — مش بيجري في بيت حد
        [SerializeField] float runSpeed = 4.2f;
        [SerializeField] float turnSmoothTime = 0.12f;
        [SerializeField] float gravity = -18f;

        [Header("أصوات وحركة")]
        [SerializeField] float stepDistance = 0.75f;

        CharacterController _cc;
        float _verticalVelocity;
        float _turnVelocity;
        float _stepAccum;
        bool _running;

        public bool CanMove { get; set; } = true;
        public bool IsRunning => _running && moveStick != null && moveStick.Value.sqrMagnitude > 0.1f;
        public float CurrentSpeed { get; private set; }

        public System.Action OnFootstep;

        void Awake() => _cc = GetComponent<CharacterController>();

        void Update()
        {
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            if (!CanMove || moveStick == null)
            {
                _cc.Move(new Vector3(0, _verticalVelocity, 0) * Time.deltaTime);
                CurrentSpeed = 0f;
                return;
            }

            Vector2 input = moveStick.Value;
            Vector3 dir = Vector3.zero;

            if (input.sqrMagnitude > 0.001f)
            {
                Vector3 fwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
                Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
                fwd.y = right.y = 0f;
                fwd.Normalize(); right.Normalize();

                dir = (fwd * input.y + right * input.x).normalized;

                float target = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, target, ref _turnVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            float speed = IsRunning ? runSpeed : walkSpeed * Mathf.Clamp01(input.magnitude);
            CurrentSpeed = speed;

            Vector3 motion = dir * speed;
            motion.y = _verticalVelocity;

            _cc.Move(motion * Time.deltaTime);

            // خطوات
            if (dir.sqrMagnitude > 0.01f && _cc.isGrounded)
            {
                _stepAccum += speed * Time.deltaTime;
                if (_stepAccum >= stepDistance)
                {
                    _stepAccum = 0f;
                    OnFootstep?.Invoke();
                }
            }
        }

        public void SetRunning(bool value) => _running = value;
    }
}
