using UnityEngine;

namespace TLC.Player
{
    /// <summary>
    /// متحكم آدم من منظور الشخص الثالث.
    /// تعجيل/تباطؤ، جاذبية، كشف أرض، دوران سلس، جري، وخُطُوات.
    /// مبنى على CharacterController (مش Rigidbody) — أدق في المساحات الضيقة زي الشقة.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("المراجع")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] Transform cameraTransform;
        [SerializeField] Animator animator;
        [SerializeField] ProceduralRig rig;

        [Header("السرعة")]
        [SerializeField] float walkSpeed = 1.55f;
        [SerializeField] float runSpeed = 4.1f;
        [SerializeField] float acceleration = 11f;
        [SerializeField] float deceleration = 15f;
        [SerializeField] float turnSmoothTime = 0.10f;
        [SerializeField] float rotationSpeed = 720f;

        [Header("الفيزياء")]
        [SerializeField] float gravity = -20f;
        [SerializeField] float groundCheckOffset = 0.15f;
        [SerializeField] LayerMask groundMask = ~0;

        [Header("خطوات")]
        [SerializeField] float stepDistance = 0.8f;

        CharacterController _cc;
        Vector3 _planarVelocity;
        float _verticalVelocity;
        float _turnVelocity;
        float _stepAccum;
        bool _grounded;

        public bool CanMove { get; set; } = true;

        /// <summary>بتتحط من HumanoidPuppet/DialogueCameraSystem وقت الكلام.</summary>
        public bool Talking { get; set; }

        /// <summary>لو true: البارامترات بتتبعت للـ Animator (لما يكون مربوط).</summary>
        public bool DrivesAnimator => animator != null;
        public float CurrentSpeed => _planarVelocity.magnitude;
        public bool IsGrounded => _grounded;
        public bool IsRunning { get; private set; }

        public System.Action OnFootstep;

        readonly System.Collections.Generic.Dictionary<string, bool> _paramCache =
            new System.Collections.Generic.Dictionary<string, bool>();

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (rig == null) rig = GetComponent<ProceduralRig>();
        }

        /// <summary>
        /// بيربط الـ Animator. البارامترات بتتفحص مرة واحدة قبل ما تتبعت،
        /// لأن إرسال بارامتر مش موجود في Unity بيطبع تحذير كل فريم — ومش عايزين
        /// الكونسول يتملى تحذيرات في اللعب العادي.
        /// </summary>
        public void SetAnimator(Animator a)
        {
            animator = a;
            _paramCache.Clear();
        }

        public void SetAnimatorParameters(string[] names)
        {
            if (names == null) return;
            _paramNames = names;
            _paramCache.Clear();
        }

        string[] _paramNames = { "Speed", "IsRunning", "Grounded", "Talking" };

        bool HasParam(string name)
        {
            if (animator == null) return false;
            if (_paramCache.TryGetValue(name, out bool cached)) return cached;

            bool has = false;
            var ps = animator.parameters;
            if (ps != null)
                for (int i = 0; i < ps.Length; i++)
                    if (ps[i].name == name) { has = true; break; }

            _paramCache[name] = has;
            return has;
        }

        void Update()
        {
            GroundCheck();

            Vector2 moveInput = (CanMove && input != null) ? input.Move : Vector2.zero;
            IsRunning = input != null && input.Run && moveInput.sqrMagnitude > 0.05f;

            Vector3 desiredDir = Vector3.zero;
            if (moveInput.sqrMagnitude > 0.0001f)
            {
                Vector3 fwd = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                fwd.y = 0f; right.y = 0f;
                fwd.Normalize(); right.Normalize();

                desiredDir = (fwd * moveInput.y + right * moveInput.x);
                if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();
            }

            float targetSpeed = IsRunning ? runSpeed : walkSpeed;
            Vector3 targetVelocity = desiredDir * targetSpeed * Mathf.Clamp01(moveInput.magnitude);

            float rate = (targetVelocity.sqrMagnitude > _planarVelocity.sqrMagnitude)
                ? acceleration
                : deceleration;

            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, rate * Time.deltaTime);

            // دوران الشخصية ناحية اتجاه الحركة
            if (desiredDir.sqrMagnitude > 0.01f)
            {
                float targetAngle = Mathf.Atan2(desiredDir.x, desiredDir.z) * Mathf.Rad2Deg;
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle,
                                                    ref _turnVelocity, turnSmoothTime);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, Quaternion.Euler(0f, angle, 0f), rotationSpeed * Time.deltaTime);
            }

            // جاذبية
            if (_grounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = _planarVelocity + Vector3.up * _verticalVelocity;
            _cc.Move(motion * Time.deltaTime);

            UpdateAnimation();
            UpdateFootsteps();
        }

        void GroundCheck()
        {
            Vector3 origin = transform.position + Vector3.up * (groundCheckOffset + 0.1f);
            _grounded = Physics.Raycast(origin, Vector3.down,
                                        groundCheckOffset + 0.25f, groundMask, QueryTriggerInteraction.Ignore)
                        || _cc.isGrounded;
        }

        void UpdateAnimation()
        {
            float speed01 = Mathf.Clamp01(CurrentSpeed / Mathf.Max(0.01f, runSpeed));

            if (rig != null) rig.SetLocomotion(speed01, IsRunning);

            if (animator != null)
            {
                if (HasParam("Speed"))     animator.SetFloat("Speed", CurrentSpeed, 0.1f, Time.deltaTime);
                if (HasParam("IsRunning")) animator.SetBool("IsRunning", IsRunning);
                if (HasParam("Grounded"))  animator.SetBool("Grounded", _grounded);
                if (HasParam("Talking"))   animator.SetBool("Talking", Talking);
            }
        }

        void UpdateFootsteps()
        {
            if (CurrentSpeed < 0.3f || !_grounded) return;

            _stepAccum += CurrentSpeed * Time.deltaTime;
            if (_stepAccum >= stepDistance)
            {
                _stepAccum = 0f;
                OnFootstep?.Invoke();
            }
        }

        /// <summary>تلي포트 آمن (بينده بيجين السرد لما يبدأ مشهد).</summary>
        public void TeleportTo(Vector3 position, float yaw)
        {
            _cc.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            _cc.enabled = true;
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 dir = worldPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }
}
