using System;
using UnityEngine;
using UnityEngine.UI;

namespace TLC.UI
{
    /// <summary>
    /// بيراقب اتجاه الشاشة (رأسي/أفقي) وبيبلّغ اللي يهمه.
    ///
    /// قرار التصميم: اللعبة بتشتغل في الاتجاهين.
    /// - أفقي: الوضع الأساسي للعب (كاميرا شخص ثالث أوسع، عصا يسار وأزرار يمين).
    /// - رأسي: مدعوم بالكامل بتخطيط مختلف، والكاميرا بتتوسّع رأسياً
    ///   عشان المجال الأفقي يفضل زي ما هو (الشخصية ما تطلعش برة الإطار).
    ///
    /// الكود هنا مش بيعتمد على Unity Editor — المنطق النقي في دوال static
    /// عشان يتراجع بالعين والاختبار الساكن.
    /// </summary>
    public class OrientationWatcher : MonoBehaviour
    {
        public static OrientationWatcher Instance { get; private set; }

        [Header("السياسة")]
        [Tooltip("لو مقفول: اللعبة بتقفل على الوضع الأفقي (شاشات اللمس الطويلة مش مريحة).")]
        [SerializeField] bool allowPortrait = true;

        [Tooltip("لو مقفول: بنقفل الاتجاه في Player Settings بس من غير ما نلمس Screen API.")]
        [SerializeField] bool applyScreenPolicy = true;

        [Tooltip("نسبة الشاشة المرجعية اللي اتصممت عليها الكاميرا.")]
        [SerializeField] float referenceAspect = 16f / 9f;

        float _baseFov = 48f;
        Camera _camera;
        bool _initialised;

        /// <summary>الشاشة أطول من عرضها.</summary>
        public bool IsPortrait { get; private set; }

        public float Aspect => (float)Screen.width / Mathf.Max(1, Screen.height);
        public bool AllowPortrait => allowPortrait;

        public event Action<bool> Changed;

        int _lastWidth, _lastHeight;

        void Awake()
        {
            Instance = this;
            Apply();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (Screen.width == _lastWidth && Screen.height == _lastHeight) return;
            Apply();
        }

        /// <summary>بتسجّل الكاميرا اللي هتتظبط عند تغيّر الاتجاه.</summary>
        public void TrackCamera(Camera cam, float baseFieldOfView)
        {
            _camera = cam;
            _baseFov = baseFieldOfView;
            ApplyCameraFov();
        }

        public void SetAllowPortrait(bool value)
        {
            allowPortrait = value;
            Apply();
        }

        public void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            bool portrait = ClassifyPortrait(_lastWidth, _lastHeight);
            bool changed = !_initialised || portrait != IsPortrait;
            IsPortrait = portrait;
            _initialised = true;

            if (applyScreenPolicy) ApplyScreenPolicy();

            ApplyCameraFov();
            ApplyCanvasScalers();

            if (changed) Changed?.Invoke(IsPortrait);
        }

        void ApplyScreenPolicy()
        {
            // AutoRotation مع السماح بالاتجاهين — الإعداد متكرر هنا
            // عشان يشتغل حتى لو Player Settings اتغيّرت بالغلط.
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = allowPortrait;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
        }

        void ApplyCameraFov()
        {
            if (_camera == null) return;
            _camera.fieldOfView = VerticalFovFor(_baseFov, Aspect, referenceAspect);
        }

        void ApplyCanvasScalers()
        {
            // مرجع الواجهة بيتبدّل مع الاتجاه عشان حجم الأزرار يفضل ثابت حسّياً
            var scalers = FindObjectsOfType<CanvasScaler>();
            for (int i = 0; i < scalers.Length; i++)
            {
                if (scalers[i] == null) continue;
                scalers[i].referenceResolution = IsPortrait
                    ? new Vector2(1080f, 1920f)
                    : new Vector2(1920f, 1080f);
            }
        }

        // ─────────────────────── منطق نقي ───────────────────────

        /// <summary>هل الأبعاد دي رأسية؟</summary>
        public static bool ClassifyPortrait(int width, int height) => height > width;

        /// <summary>
        /// FOV رأسي بيحافظ على المجال الأفقي ثابت مهما كانت نسبة الشاشة.
        /// Unity بيحسب FOV رأسياً، فالشاشة الرأسية بتضيّع المجال الأفقي —
        /// فبنوسّع الرأسي بنفس النسبة.
        /// </summary>
        public static float VerticalFovFor(float baseFov, float aspect, float referenceAspect)
        {
            if (baseFov <= 0f) return 60f;
            if (referenceAspect <= 0.01f) referenceAspect = 16f / 9f;
            if (aspect <= 0.01f) aspect = referenceAspect;

            float halfV = baseFov * Mathf.Deg2Rad * 0.5f;
            float halfH = Mathf.Atan(Mathf.Tan(halfV) * referenceAspect);
            float wanted = 2f * Mathf.Atan(Mathf.Tan(halfH) / aspect) * Mathf.Rad2Deg;
            return Mathf.Clamp(wanted, 30f, 100f);
        }

        /// <summary>بعد تعديل الواجهة: هامش الأمان المطلوب تحت/فوق (ثابت مقروء).</summary>
        public static float UiMargin(bool portrait) => portrait ? 0.06f : 0.04f;
    }
}
