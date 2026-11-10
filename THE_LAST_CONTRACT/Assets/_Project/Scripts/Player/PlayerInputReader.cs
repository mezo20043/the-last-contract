using UnityEngine;

namespace TLC.Player
{
    /// <summary>
    /// مدخلات موحّدة: كيبورد + ماوس + واجهة لمس.
    ///
    /// كان فيه باج حقيقي هنا: الإدخال اللمسي كان ورا فلاج <c>mobileMode</c>
    /// مش بيتفعّل من أي مكان، يعني على الموبايل كان بيتجاهل العصا تماماً.
    /// دلوقتي المصدرين بيتجمّعوا كل فريم — مفيش فلاج ينسى حد يفعّله،
    /// والاثنين بيشتغلوا في نفس الوقت (مفيد في اختبار المحرر).
    ///
    /// التجميع بياخد الإدخال الأكبر مش المجموع: لو اللاعب بيسحب العصا
    /// وضغط زر كيبورد بالغلط، الحركة ما تتضاعفش.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("الكيبورد والماوس")]
        [SerializeField] bool invertMouseY = false;
        [SerializeField] float mouseSensitivity = 2.6f;

        Vector2 _keyboardMove, _mobileMove;
        Vector2 _mouseLook, _mobileLook;
        bool _keyboardRun, _mobileRun;
        bool _mobileInteract, _mobileAdvance;

        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Run { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool AdvancePressed { get; private set; }

        /// <summary>آخر إدخال حركة جه من واجهة اللمس — بيتستخدم في التشخيص.</summary>
        public bool TouchActive { get; private set; }

        /// <summary>بيتوقف لما الحوار أو السينمائية شغالة.</summary>
        public bool GameplayEnabled { get; set; } = true;

        // ── واجهة الموبايل بتنادي الدوال دي ──
        public void SetMobileMove(Vector2 v) => _mobileMove = Vector2.ClampMagnitude(v, 1f);
        public void SetMobileLook(Vector2 v) => _mobileLook = v;
        public void SetMobileRun(bool v) => _mobileRun = v;
        public void TriggerInteract() => _mobileInteract = true;
        public void TriggerAdvance() => _mobileAdvance = true;

        /// <summary>بيمسح كل الإدخال اللمسي — بيتنادى لما الواجهة تتقفل.</summary>
        public void ClearMobileInput()
        {
            _mobileMove = Vector2.zero;
            _mobileLook = Vector2.zero;
            _mobileRun = false;
            _mobileInteract = false;
            _mobileAdvance = false;
        }

        void Update()
        {
            InteractPressed = false;
            AdvancePressed = false;

            ReadKeyboard();
            ReadMouse();
            Combine();

            if (_mobileInteract) InteractPressed = true;
            if (_mobileAdvance) AdvancePressed = true;

            _mobileInteract = false;
            _mobileAdvance = false;
            _mobileLook = Vector2.zero;   // دفعة لمس = فريم واحد
        }

        void ReadKeyboard()
        {
            float x = 0f, z = 0f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;

            _keyboardMove = new Vector2(x, z);
            if (_keyboardMove.sqrMagnitude > 1f) _keyboardMove.Normalize();

            _keyboardRun = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.E)) _mobileInteract = true;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) _mobileAdvance = true;
        }

        void ReadMouse()
        {
            if (!Input.GetMouseButton(1))
            {
                _mouseLook = Vector2.zero;
                return;
            }

            float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * (invertMouseY ? 1f : -1f);
            _mouseLook = new Vector2(mx, my);
        }

        void Combine()
        {
            TouchActive = _mobileMove.sqrMagnitude > 0.01f || _mobileLook.sqrMagnitude > 0.01f;

            Vector2 move = _mobileMove.sqrMagnitude > _keyboardMove.sqrMagnitude
                ? _mobileMove
                : _keyboardMove;

            Move = GameplayEnabled ? move : Vector2.zero;
            Run = GameplayEnabled && (_mobileRun || _keyboardRun);
            Look = _mobileLook.sqrMagnitude > 0.01f ? _mobileLook : _mouseLook;
        }
    }
}
