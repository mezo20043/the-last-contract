using TMPro;
using UnityEngine;

namespace TLC.Settings
{
    /// <summary>
    /// بيركّب على أي عنصر TextMeshProUGUI وبيحوّل أي نص عربي بيتكتب فيه
    /// (تشكيل + ترتيب بصري) قبل ما يترسم.
    ///
    /// ليه مفيش `text` hook؟ لأن خاصية text في TMP مش virtual. فبنراقب النص:
    ///   • لو اتغيّر عن آخر نص عربي «خام» شفناه ⇒ ده نص جديد ⇒ بنشكّله.
    ///   • لو النص الحالي هو نفس الناتج اللي حناه ⇒ مفيش حلقة لا نهائية.
    ///
    /// وبيشتغل مع أي كود بيكتب `tmp.text = "..."` من غير ما نلمس كل سطر في المشروع.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArabicTextBinder : MonoBehaviour
    {
        TMP_Text _tmp;
        string _lastRaw;
        string _lastFixed;

        [Tooltip("لو مقفول: النص بيترسم زي ما هو (إنجليزي/أرقام)")]
        public bool Enabled = true;

        void Awake() => _tmp = GetComponent<TMP_Text>();

        void OnEnable() => _tmp = _tmp != null ? _tmp : GetComponent<TMP_Text>();

        void LateUpdate()
        {
            if (!Enabled || _tmp == null) return;

            string current = _tmp.text;

            // النص الحالي هو ناتجنا ⇒ مفيش حاجة نعملها
            if (!string.IsNullOrEmpty(_lastFixed) && current == _lastFixed) return;

            // نفس النص الخام اللي شفناه قبل كده (مثلاً TMP أعاد الرسم) ⇒ نرجّع الناتج
            if (current == _lastRaw && _lastFixed != null)
            {
                _tmp.text = _lastFixed;
                return;
            }

            _lastRaw = current;
            _lastFixed = ArabicShaper.Fix(current);
            _tmp.text = _lastFixed;
        }

        /// <summary>للاستخدام المباشر من الكود اللي عايز يشكّل نص من غير مكوّن.</summary>
        public static string Fix(string raw) => ArabicShaper.Fix(raw);
    }
}
