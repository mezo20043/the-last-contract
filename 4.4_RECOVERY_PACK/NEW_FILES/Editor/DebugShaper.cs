// 4.4 RECOVERY — DebugShaper.cs — **مُعاد حرفيًا** (أداة تشخيص التشكيل العربي)
using System.Text;
using UnityEditor;
using UnityEngine;
using TLC.Settings;

namespace TLC.EditorTools
{
    /// <summary>تشخيص: إيه اللي بيتحوّل فعلاً للنص العربي قبل TMP؟ (يطلع code points)</summary>
    public static class DebugShaper
    {
        [MenuItem("TLC/تشخيص التشكيل")]
        public static void Run()
        {
            const string logical = "لعبة جديدة";
            string fixedText = ArabicShaper.Fix(logical);

            Debug.Log($"[TLC/Shaper] الخام   = {Cps(logical)}");
            Debug.Log($"[TLC/Shaper] الناتج = {Cps(fixedText)}");
            Debug.Log($"[TLC/Shaper] ناتج مرتين = {Cps(ArabicShaper.Fix(fixedText))}");
            Debug.Log($"[TLC/Shaper] متساوي مع الخام؟ {fixedText == logical} · معكوس؟ {fixedText == Rev(logical)}");
            EditorApplication.Exit(0);
        }

        static string Rev(string s)
        {
            var a = s.ToCharArray();
            System.Array.Reverse(a);
            return new string(a);
        }

        static string Cps(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s) sb.Append($"U+{(int)c:X4} ");
            return sb.ToString().Trim();
        }
    }
}
