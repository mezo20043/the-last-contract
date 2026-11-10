using TMPro;
using UnityEngine;

namespace TLC.Settings
{
    /// <summary>
    /// الخط العربي: بيحمّل Amiri (رخصة OFL) ويولّد منه Font Asset ديناميكي
    /// لـ TextMeshPro وقت التشغيل — من غير أي خطوة يدوية في المحرر.
    ///
    /// ليه ديناميكي؟ عشان الجليفات العربية بتتولّد عند الطلب، فمفيش حدود
    /// لتغطية الحروف ولا حاجة لإعداد أطلس مسبق.
    /// </summary>
    public static class ArabicFont
    {
        public const string ResourcePath = "Fonts/Amiri-Regular";

        static TMP_FontAsset _asset;
        static bool _attempted;

        public static TMP_FontAsset Asset
        {
            get { Ensure(); return _asset; }
        }

        public static bool IsAvailable
        {
            get { Ensure(); return _asset != null; }
        }

        static void Ensure()
        {
            if (_attempted) return;
            _attempted = true;

            // ١) خط TMP مبني مسبقًا في المحرر (الطريق الأساسي — أسرع وأضمن)
            var prebuilt = Resources.Load<TMP_FontAsset>("Fonts/Amiri SDF");
            if (prebuilt != null)
            {
                _asset = prebuilt;
                Debug.Log("[TLC/Font] اتحمّل خط Amiri SDF الجاهز — RTL جاهز.");
                return;
            }

            var font = Resources.Load<Font>(ResourcePath);
            if (font == null)
            {
                Debug.LogWarning($"[TLC/Font] '{ResourcePath}' مش موجود — النص العربي هيظهر مربعات.");
                return;
            }

            // التوليد بيعتمد على وجود «TMP Settings». لو مش موجود بيرمي
            // NullReferenceException جوه TMP. أي استثناء هنا كان بيوقف بناء
            // الواجهة كلها (المنيو كانت بتطلع بزراير صفر!). عشان كده محاطينه.
            try
            {
                _asset = TMP_FontAsset.CreateFontAsset(font);
                if (_asset != null)
                {
                    _asset.name = "Amiri SDF (dynamic)";
                    Debug.Log("[TLC/Font] اتحمّل الخط العربي Amiri — RTL جاهز.");
                }
            }
            catch (System.Exception e)
            {
                _asset = null;
                Debug.LogWarning("[TLC/Font] ⚠️ تعذر توليد خط Amiri (" + e.GetType().Name +
                                 ") — الواجهة هتتبني بخط TMP الافتراضي.");
            }
        }

        /// <summary>يطبّق الخط على عنصر نص — آمن لو الخط مش موجود.</summary>
        public static void Apply(TextMeshProUGUI text)
        {
            if (text == null) return;
            var a = Asset;
            if (a != null) text.font = a;
        }

        public static void ApplyTo(GameObject root)
        {
            if (root == null) return;
            foreach (var t in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                Apply(t);
        }

        /// <summary>للإعادة بين تشغيلات الاختبار.</summary>
        public static void ResetForTests()
        {
            _asset = null;
            _attempted = false;
        }
    }
}
