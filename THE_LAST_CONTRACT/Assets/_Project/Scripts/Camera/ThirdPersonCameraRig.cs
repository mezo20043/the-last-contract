using UnityEngine;
using TLC.Player;

namespace TLC.CameraRig
{
    /// <summary>
    /// كاميرا اللعب من ورا الكتف — النسخة المحسّنة (v2.0).
    ///
    /// اللي اتغيّر عن v1.9:
    ///  • اصطدام متعدد العينات (٣ أشعة + SphereCast) بدل عينة واحدة — بتمنع الكاميرا
    ///    تدخل الحيطة من الجنب لما تكون لازقة فيها.
    ///  • استثناء جسم آدم نفسه من الأشعة — الكاميرا مابقتش "تشوف" آدم كعائق فتقفز.
    ///  • دخول فوري عند الاصطدام وخروج تدريجي (Snap-In / Ease-Out) — الإحساس اللي
    ///    في الألعاب الكبيرة: الكاميرا تترد بسرعة وترجع بهدوء.
    ///  • وضع الأماكن الضيقة: لو المساحة أقل من 1.4م بتقصر المسافة، ترفع الميل،
    ///    وتضيّق الـFOV شوية عشان آدم يفضل في الكادر وميتقطعش.
    ///  • هامش أمان من السطح (skin) وحد أدنى مضمون — مفيش "الكاميرا جوه راس آدم".
    ///  • تبديل الكتف (SwapShoulder) و لقطة "نظرة سريعة ورا" (LookBack).
    /// </summary>
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        [Header("الهدف")]
        [SerializeField] Transform target;
        [SerializeField] PlayerInputReader input;
        [SerializeField] float heightOffset = 1.35f;
        [Tooltip("ارتفاع نقطة التصويب فوق الأرض (بتتظبط أوتوماتيك على طول الشخصية)")]
        [SerializeField] float aimHeight = 1.55f;

        [Header("المدار")]
        [SerializeField] float distance = 3.1f;
        [SerializeField] float shoulderOffset = 0.30f;
        [SerializeField] float minPitch = -28f;
        [SerializeField] float maxPitch = 62f;
        [SerializeField] float followLerp = 12f;
        [Tooltip("سرعة الرجوع بعد ما العقبة تختفي (أبطأ من الدخول)")]
        [SerializeField] float recoverLerp = 6f;

        [Header("منع الاختراق")]
        [SerializeField] float collisionRadius = 0.24f;
        [Tooltip("مسافة أمان بين الكاميرا وأي سطح")]
        [SerializeField] float surfaceSkin = 0.12f;
        [SerializeField] float minDistance = 0.55f;
        [SerializeField] LayerMask collisionMask = ~0;
        [Tooltip("حوائط بتتشاف بالسكربت — بتستثنى من الأشعة (جسم اللاعب)")]
        [SerializeField] bool ignorePlayerColliders = true;

        [Header("الأماكن الضيقة")]
        [SerializeField] float tightSpaceDistance = 1.45f;
        [SerializeField] float tightPitchBonus = 14f;
        [SerializeField] float tightFovBonus = -6f;

        Camera _cam;
        float _yaw;
        float _pitch = 8f;
        float _currentDistance;
        float _shoulderSide = 1f;     // +1 يمين · -1 شمال
        float _lookBackTimer;
        float _tightBlend;            // 0 = مساحة مفتوحة · 1 = مكان ضيق
        float _baseFov = 48f;
        float _dynamicFovOffset;

        readonly RaycastHit[] _hits = new RaycastHit[8];

        public Camera GameplayCamera => _cam;
        public float CurrentDistance => _currentDistance;
        public bool IsTightSpace => _tightBlend > 0.5f;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) _cam = GetComponentInChildren<Camera>();
            _currentDistance = distance;
            if (_cam != null)
            {
                _baseFov = _cam.fieldOfView;
                if (_cam.transform != transform) _cam.transform.SetParent(transform, false);
            }
        }

        void Start()
        {
            if (target != null) SnapBehind();
        }

        public void SetTarget(Transform t, bool snap = true)
        {
            target = t;
            if (snap) SnapBehind();
        }

        /// <summary>بضبط نقطة التصويب على طول الشخصية الحقيقي (مش رقم ثابت).</summary>
        public void AutoFitToTarget()
        {
            if (target == null) return;

            // الطول من الـCharacterController الأول: حدود الـSkinnedMeshRenderer بتتضخم
            // لما العظام تتلفّل (وبعدين الكاميرا تطلع فوق السقف)، فالرقم الفيزيائي أوثق.
            var cc = target.GetComponent<CharacterController>();
            float h;
            if (cc != null) h = cc.height;
            else
            {
                var rends = target.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) return;
                var bb = rends[0].bounds;
                foreach (var r in rends) bb.Encapsulate(r.bounds);
                h = Mathf.Max(1.2f, bb.max.y - target.position.y);
            }
            h = Mathf.Clamp(h, 1.2f, 2.4f);
            aimHeight = h * 0.86f;
            heightOffset = h * 0.75f;
        }

        public void SnapBehind()
        {
            if (target == null) return;
            _yaw = target.eulerAngles.y;
            _pitch = 8f;
            _tightBlend = 0f;
            ApplyTransform(true);
        }

        public void SwapShoulder() => _shoulderSide = -_shoulderSide;
        public void LookBack(float seconds = 1.2f) => _lookBackTimer = seconds;

        /// <summary>إزاحة FOV مؤقتة (بيستخدمها مدير السينمائيات للحظات التوتر).</summary>
        public void PushFov(float offset, float smoothing = 4f) => _dynamicFovOffset = Mathf.Lerp(_dynamicFovOffset, offset, Time.deltaTime * smoothing);

        void LateUpdate()
        {
            if (target == null || _cam == null) return;

            if (input != null)
            {
                Vector2 look = input.Look;
                float mul = _lookBackTimer > 0f ? -1f : 1f;
                _yaw += look.x * mul;
                _pitch -= look.y * mul;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
            }

            if (_lookBackTimer > 0f) _lookBackTimer -= Time.deltaTime;

            ApplyTransform(false);
        }

        void ApplyTransform(bool instant)
        {
            Vector3 pivot = target.position + Vector3.up * heightOffset;

            float tightTarget = _currentDistance < tightSpaceDistance ? 1f : 0f;
            _tightBlend = instant ? tightTarget : Mathf.Lerp(_tightBlend, tightTarget, 4f * Time.deltaTime);

            float pitch = _pitch + tightPitchBonus * _tightBlend;
            Quaternion rot = Quaternion.Euler(pitch, _yaw, 0f);

            Vector3 desiredDir = rot * Vector3.back;
            float shoulder = shoulderOffset * _shoulderSide * (1f - 0.45f * _tightBlend);
            Vector3 shoulderVec = rot * new Vector3(shoulder, 0f, 0f);
            Vector3 pivotShifted = pivot + shoulderVec;

            float allowed = SolveDistance(pivotShifted, desiredDir, distance);

            // Snap-in فوري · Ease-out تدريجي
            if (instant)
                _currentDistance = allowed;
            else if (allowed < _currentDistance)
                _currentDistance = Mathf.Lerp(_currentDistance, allowed, Mathf.Clamp01(22f * Time.deltaTime));
            else
                _currentDistance = Mathf.Lerp(_currentDistance, allowed, Mathf.Clamp01(recoverLerp * Time.deltaTime));

            Vector3 finalPos = pivotShifted + desiredDir * _currentDistance;

            // شبكة أمان أخيرة: لو الكاميرا لسه جوه أي مجسم، نرفعها لفوق نقطة الارتكاز
            if (Physics.CheckSphere(finalPos, collisionRadius * 0.75f, collisionMask, QueryTriggerInteraction.Ignore))
            {
                Vector3 lifted = pivotShifted + desiredDir * Mathf.Max(minDistance, _currentDistance * 0.7f) + Vector3.up * 0.18f;
                finalPos = lifted;
            }

            if (instant)
            {
                transform.position = finalPos;
                transform.rotation = rot;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, finalPos, followLerp * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, followLerp * Time.deltaTime);
            }

            if (_cam != null)
            {
                float targetFov = _baseFov + tightFovBonus * _tightBlend + _dynamicFovOffset;
                _cam.fieldOfView = instant ? targetFov : Mathf.Lerp(_cam.fieldOfView, targetFov, 6f * Time.deltaTime);
                _dynamicFovOffset = Mathf.Lerp(_dynamicFovOffset, 0f, 3f * Time.deltaTime);
            }
        }

        /// <summary>
        /// بيحسب أقصى مسافة مسموحة للكاميرا: ٥ عينات حول محور التصويب + عينة مركزية،
        /// ويختار الأقرب. الاستثناءات: أي كوليدر تابع لآدم.
        /// </summary>
        float SolveDistance(Vector3 pivot, Vector3 dir, float maxDistance)
        {
            float best = maxDistance;

            best = Mathf.Min(best, Sample(pivot, dir, maxDistance));

            // عينات جانبية بتكشف الحيطة اللي جوه زاوية الكاميرا
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, dir).normalized;
            Vector3 sideUp = Vector3.Cross(dir, right).normalized;

            for (int i = 0; i < 4; i++)
            {
                float ang = (90f * i + 45f) * Mathf.Deg2Rad;
                Vector3 off = (right * Mathf.Cos(ang) + sideUp * Mathf.Sin(ang)) * collisionRadius;
                best = Mathf.Min(best, Sample(pivot + off, dir, maxDistance));
            }

            return Mathf.Clamp(best, minDistance, maxDistance);
        }

        float Sample(Vector3 origin, Vector3 dir, float maxDistance)
        {
            int count = Physics.SphereCastNonAlloc(origin, collisionRadius, dir, _hits, maxDistance,
                                                   collisionMask, QueryTriggerInteraction.Ignore);
            float best = maxDistance;
            for (int i = 0; i < count; i++)
            {
                var h = _hits[i];
                if (h.collider == null) continue;
                if (ignorePlayerColliders && target != null && h.collider.transform.IsChildOf(target)) continue;
                float d = h.distance - surfaceSkin;
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>مسافة الذراع (بتستخدمها لقطات الإثبات عشان آدم يبان كامل).</summary>
        public void SetDistance(float d) { distance = Mathf.Clamp(d, minDistance, 9f); }

        public void SetOrbit(float yaw, float pitch, bool instant = false)
        {
            _yaw = yaw;
            _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            if (instant) ApplyTransform(true);
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;
    }
}
