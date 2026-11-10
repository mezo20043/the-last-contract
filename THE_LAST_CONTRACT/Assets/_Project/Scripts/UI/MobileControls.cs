using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TLC.Player;
using TLC.Settings;

namespace TLC.UI
{
    /// <summary>
    /// واجهة تحكم الموبايل الكاملة.
    ///
    /// ═══ كان فيه باج ═══
    /// النسخة القديمة من الملف ده كانت موجودة بس **عمرها ما اتبنت**:
    /// فلاج <c>buildMobileControls</c> في GameBootstrap كان معرّف ومش مستخدم،
    /// فمفيش عصا ولا زر تفاعل ولا منطقة كاميرا على الموبايل إطلاقاً.
    /// وكمان كان فيه فلاج <c>mobileMode</c> في PlayerInputReader
    /// بيتحكم في قراءة العصا ومش بيتفعّل من أي مكان.
    /// الاتنين اتصلّحوا.
    ///
    /// ═══ اللي فيها دلوقتي ═══
    /// • عصا افتراضية (Virtual Joystick) بتقرا من ControlSettings الحجم والجهة.
    /// • منطقة سحب للكاميرا بتغطي النص التاني من الشاشة.
    /// • زر تفاعل بيبان بس لما فيه حاجة قابلة للتفاعل قريبة.
    /// • زر جري (ضغط مستمر، أو تلقائي من الإعدادات).
    /// • زر متابعة للحوار.
    /// • تخطيطين: أفقي ورأسي، بيتبدّلوا تلقائياً مع اتجاه الشاشة.
    ///
    /// كل الربط بالحركة بيمرّ من PlayerInputReader (المصدر الوحيد للإدخال)،
    /// والـ ThirdPersonController بيقرا منه — فمفيش مسار تاني للحركة.
    /// </summary>
    public class MobileControls : MonoBehaviour
    {
        [Header("الظهور")]
        [Tooltip("بيظهر على أجهزة اللمس دايماً، ولو مفعّل بيظهر في المحرر كمان للاختبار.")]
        [SerializeField] bool showOnDesktop = true;

        [Header("المقاسات (وحدات مرجع الواجهة)")]
        [SerializeField] float stickSize = 300f;
        [SerializeField] float handleSize = 128f;
        [SerializeField] float handleRange = 108f;
        [SerializeField] float lookSensitivity = 0.35f;

        [Header("المظهر")]
        [SerializeField] Color stickTint = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] Color handleTint = new Color(0.85f, 0.71f, 0.42f, 0.34f);

        PlayerInputReader _input;
        DialogueUI _dialogue;
        ThirdPersonController _controller;

        Canvas _canvas;
        RectTransform _safe, _stickRoot, _handle, _lookArea;
        Button _interactBtn, _runBtn, _advanceBtn;
        Image _interactImg;

        JoystickBehaviour _stick;
        LookPadBehaviour _lookPad;

        bool _interactAvailable;
        bool _gameplayVisible = true;
        bool _autoRunLatched;

        public bool IsBuilt { get; private set; }
        public bool IsPortrait { get; private set; }
        public bool InteractAvailable => _interactAvailable;
        public RectTransform StickRoot => _stickRoot;
        public LookPadBehaviour LookPad => _lookPad;

        /// <summary>بتتنادى لو الإعدادات اتغيّرت من القائمة.</summary>
        public event Action SettingsApplied;

        // ───────────────────────── البناء ─────────────────────────

        /// <summary>
        /// بيبني الواجهة. البارامترات كلها اختيارية — أي واحد بيعدي null بيتشال من الربط،
        /// والواجهة تفضل شغالة بالمتبقي.
        /// </summary>
        public void Build(PlayerInputReader input, DialogueUI dialogue = null,
                          ThirdPersonController controller = null)
        {
            if (IsBuilt) return;

            _input = input;
            _dialogue = dialogue;
            _controller = controller;

            bool touch = Application.isMobilePlatform || showOnDesktop;
            if (!touch)
            {
                Debug.Log("[TLC/Mobile] جهاز مش لمسي — واجهة التحكم مش هتتبني.");
                return;
            }

            UIFactory.EnsureEventSystem();

            _canvas = UIFactory.CreateCanvas("UI_Mobile", 5);
            _canvas.transform.SetParent(transform, false);
            _safe = UIFactory.CreateSafeArea(_canvas);

            BuildLookArea();
            BuildStick();
            BuildButtons();

            IsBuilt = true;

            ControlSettings.Changed += ApplySettings;

            if (OrientationWatcher.Instance != null)
            {
                IsPortrait = OrientationWatcher.Instance.IsPortrait;
                OrientationWatcher.Instance.Changed += OnOrientationChanged;
            }
            else
            {
                IsPortrait = OrientationWatcher.ClassifyPortrait(Screen.width, Screen.height);
            }

            ApplyLayout();
            ApplySettings();

            // على الديسكتوب مفيش لمس: عصا التحكم ومنطقة النظر مش مستخدمين
            // وبيبانوا كمستطيل رمادي فاضي (Placeholder). بنخفييهم ونسيب
            // أزرار «تفاعل» و«التالي» بس. على الموبايل بيفضلوا زي ما هما.
            if (!Application.isMobilePlatform)
            {
                // بنخفي الصور نفسها (مش الكائن) عشان السلوك يفضل شغال ومايحصلش
                // أي NullReference في أنظمة الإدخال اللي بتقرأ من العصا.
                if (_stickRoot != null)
                {
                    foreach (var im in _stickRoot.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                    {
                        var c = im.color; c.a = 0f; im.color = c; im.raycastTarget = false;
                    }
                }
                if (_lookArea != null)
                {
                    var img = _lookArea.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) { var c = img.color; c.a = 0f; img.color = c; img.raycastTarget = false; }
                }
            }
            SetInteractAvailable(false);
            SetGameplayVisible(true);

            Debug.Log($"[TLC/Mobile] واجهة التحكم اتبنت · تخطيط: {(IsPortrait ? "رأسي" : "أفقي")}" +
                      $" · عصا: {(ControlSettings.StickSide == JoystickSide.Left ? "يسار" : "يمين")}");
        }

        void OnDestroy()
        {
            ControlSettings.Changed -= ApplySettings;
            if (OrientationWatcher.Instance != null)
                OrientationWatcher.Instance.Changed -= OnOrientationChanged;
        }

        void BuildLookArea()
        {
            _lookArea = UIFactory.CreatePanel(_safe, "LookArea",
                new Vector2(0.35f, 0.25f), Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0f, 0f, 0f, 0f));
            _lookArea.GetComponent<Image>().raycastTarget = true;

            _lookPad = _lookArea.gameObject.AddComponent<LookPadBehaviour>();
            _lookPad.OnDelta = d => { if (_input != null) _input.SetMobileLook(d); };
        }

        void BuildStick()
        {
            _stickRoot = UIFactory.CreatePanel(_safe, "Joystick",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, stickTint);
            _stickRoot.pivot = new Vector2(0.5f, 0.5f);

            var bg = _stickRoot.GetComponent<Image>();
            bg.sprite = null;
            bg.raycastTarget = true;
            bg.color = stickTint;

            _handle = UIFactory.CreatePanel(_stickRoot, "Handle",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, handleTint);
            _handle.pivot = new Vector2(0.5f, 0.5f);
            _handle.GetComponent<Image>().raycastTarget = false;

            _stick = _stickRoot.gameObject.AddComponent<JoystickBehaviour>();
            _stick.Handle = _handle;
            _stick.HandleRange = handleRange;
            _stick.OnValue = v => { if (_input != null) _input.SetMobileMove(v); };
        }

        void BuildButtons()
        {
            // زر التفاعل
            _interactBtn = UIFactory.CreateButton(_safe, "InteractButton", "تفاعل",
                new Vector2(180f, 180f), 36f);
            _interactImg = _interactBtn.GetComponent<Image>();
            _interactBtn.onClick.AddListener(() =>
            {
                if (_input != null) _input.TriggerInteract();
            });

            // زر الجري (ضغط مستمر)
            _runBtn = UIFactory.CreateButton(_safe, "RunButton", "جري",
                new Vector2(150f, 150f), 34f);
            var hold = _runBtn.gameObject.AddComponent<HoldButtonBehaviour>();
            hold.OnHold = pressing =>
            {
                if (_input == null) return;
                _input.SetMobileRun(pressing || (ControlSettings.AutoRun && _gameplayVisible));
            };

            // زر المتابعة
            _advanceBtn = UIFactory.CreateButton(_safe, "AdvanceButton", "التالي",
                new Vector2(240f, 104f), 34f);
            _advanceBtn.onClick.AddListener(() =>
            {
                if (_dialogue != null) _dialogue.OnAdvancePressed();
                else if (_input != null) _input.TriggerAdvance();
            });
        }

        // ───────────────────────── التخطيط ─────────────────────────

        void OnOrientationChanged(bool portrait)
        {
            IsPortrait = portrait;
            ApplyLayout();
            Debug.Log($"[TLC/Mobile] اتجاه الشاشة: {(portrait ? "رأسي" : "أفقي")} — التخطيط اتظبط.");
        }

        /// <summary>بيحسب مواضع العناصر حسب الاتجاه وجهة العصا.</summary>
        public void ApplyLayout()
        {
            if (!IsBuilt) return;

            bool stickLeft = ControlSettings.StickSide == JoystickSide.Left;
            float stickX = stickLeft ? 0.14f : 0.86f;
            float buttonX = stickLeft ? 0.86f : 0.14f;

            // العصا
            float stickAnchorY = IsPortrait ? 0.16f : 0.22f;
            float side = stickSize * ControlSettings.StickScale;

            Anchor(_stickRoot, new Vector2(stickX, stickAnchorY));
            _stickRoot.sizeDelta = new Vector2(side, side);
            _handle.sizeDelta = new Vector2(handleSize * ControlSettings.StickScale,
                                            handleSize * ControlSettings.StickScale);
            _stick.HandleRange = handleRange * ControlSettings.StickScale;

            // منطقة الكاميرا: النص التاني من الشاشة (مش فوق الأزرار)
            if (stickLeft)
                SetArea(_lookArea, new Vector2(0.34f, IsPortrait ? 0.30f : 0.26f), Vector2.one);
            else
                SetArea(_lookArea, new Vector2(0f, IsPortrait ? 0.30f : 0.26f), new Vector2(0.66f, 1f));

            // الأزرار
            float interactY = IsPortrait ? 0.32f : 0.44f;
            float runY = IsPortrait ? 0.18f : 0.22f;

            Anchor(_interactBtn.GetComponent<RectTransform>(), new Vector2(buttonX, interactY));
            Anchor(_runBtn.GetComponent<RectTransform>(), new Vector2(buttonX, runY));

            var advanceRt = _advanceBtn.GetComponent<RectTransform>();
            advanceRt.anchorMin = advanceRt.anchorMax = new Vector2(0.5f, IsPortrait ? 0.055f : 0.07f);
            advanceRt.pivot = new Vector2(0.5f, 0.5f);
            advanceRt.anchoredPosition = Vector2.zero;
        }

        static void Anchor(RectTransform rt, Vector2 normalized)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = normalized;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        static void SetArea(RectTransform rt, Vector2 min, Vector2 max)
        {
            if (rt == null) return;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ───────────────────────── الإعدادات ─────────────────────────

        /// <summary>بيقرا ControlSettings ويطبّقها من غير إعادة بناء.</summary>
        public void ApplySettings()
        {
            if (!IsBuilt) return;

            _autoRunLatched = ControlSettings.AutoRun;
            if (_input != null) _input.SetMobileRun(_autoRunLatched && _gameplayVisible);

            if (_lookPad != null)
                _lookPad.sensitivity = lookSensitivity * ControlSettings.LookSensitivity;

            ApplyLayout();
            SettingsApplied?.Invoke();
        }

        // ───────────────────────── الحالة ─────────────────────────

        /// <summary>زر التفاعل بيظهر بس لما يكون فيه هدف قريب.</summary>
        public void SetInteractAvailable(bool available)
        {
            _interactAvailable = available;
            if (_interactBtn == null) return;

            _interactBtn.interactable = available;
            if (_interactImg != null)
            {
                var c = _interactImg.color;
                c.a = available ? 0.92f : 0.25f;
                _interactImg.color = c;
            }
        }

        /// <summary>بيخفي/بيظهر مجموعة اللعب (وقت الحوار أو السينمائية).</summary>
        public void SetGameplayVisible(bool visible)
        {
            _gameplayVisible = visible;

            if (_stickRoot != null) _stickRoot.gameObject.SetActive(visible);
            if (_runBtn != null) _runBtn.gameObject.SetActive(visible);
            if (_interactBtn != null) _interactBtn.gameObject.SetActive(visible);

            if (!visible) _input?.ClearMobileInput();
        }

        void Update()
        {
            if (!IsBuilt || _input == null) return;

            // الربط المباشر: العصا بتختفي لما الحركة تتقفل من أي نظام تاني
            // (السينمائية بتقفل CanMove، والحوار بيقفل GameplayEnabled).
            bool canPlay = _input.GameplayEnabled &&
                           (_controller == null || _controller.CanMove);
            if (canPlay != _gameplayVisible) SetGameplayVisible(canPlay);

            // الجري التلقائي: بنبعته للإدخال كل فريم عشان يفضل شغال بعد أي إعادة ضبط
            if (ControlSettings.AutoRun != _autoRunLatched) ApplySettings();
        }

        void OnDisable()
        {
            if (_input != null) _input.ClearMobileInput();
        }
    }

    /// <summary>اسحب في أي مكان في المنطقة دي = دوران كاميرا.</summary>
    public class LookPadBehaviour : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public Action<Vector2> OnDelta;
        [SerializeField] public float sensitivity = 0.35f;

        public void OnPointerDown(PointerEventData e) => OnDelta?.Invoke(Vector2.zero);

        public void OnDrag(PointerEventData e) => OnDelta?.Invoke(e.delta * sensitivity);
    }

    /// <summary>عصاية افتراضية: الضغط والدوران والرفع.</summary>
    public class JoystickBehaviour : MonoBehaviour,
        IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Handle;
        public float HandleRange = 108f;
        public Action<Vector2> OnValue;

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            var rt = transform as RectTransform;
            if (rt == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rt, e.position, e.pressEventCamera, out var local))
                return;

            Vector2 half = rt.rect.size * 0.5f;
            if (half.x < 1f || half.y < 1f) return;

            Vector2 value = new Vector2(local.x / half.x, local.y / half.y);
            value = Vector2.ClampMagnitude(value, 1f);

            if (Handle != null)
                Handle.anchoredPosition = value * (half.x * 0.45f);

            OnValue?.Invoke(value);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (Handle != null) Handle.anchoredPosition = Vector2.zero;
            OnValue?.Invoke(Vector2.zero);
        }
    }

    /// <summary>زر بيتقري وهو مضغوط (للجري).</summary>
    public class HoldButtonBehaviour : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Action<bool> OnHold;
        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData e)
        {
            IsHeld = true;
            OnHold?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsHeld = false;
            OnHold?.Invoke(false);
        }
    }
}
