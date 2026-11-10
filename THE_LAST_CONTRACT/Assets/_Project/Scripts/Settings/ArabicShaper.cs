using System.Collections.Generic;
using System.Text;

namespace TLC.Settings
{
    /// <summary>
    /// تشكيل عربي + ترتيب ثنائي الاتجاه (Bidi) يدويًا قبل تمرير النص لـTextMeshPro.
    ///
    /// ليه؟
    ///   • TMP عنده `isRightToLeftText` بس بيعكس الترتيب من غير ما يعمل تشكيل (contextual
    ///     shaping) ولا bidi كامل ⇒ النص بيطلع معكوس الحروف أو مقطّع.
    ///   • فونت Amiri فيه بلوك Arabic Presentation Forms-B (140 محرف) — يعني عندنا
    ///     الأشكال الجاهزة (معزول/أولي/وسطي/نهائي) + ليجاتشر لام-ألف.
    ///
    /// الخطوات:
    ///   ١) Shaping: كل حرف عربي بيتحوّل لشكله الصح حسب الحرف اللي قبله/بعده،
    ///      مع ليجاتشر لا / لأ / لإ / لآ، والحروف اللي مابتوصلش بعدها (ا د ذ ر ز و…).
    ///   ٢) Bidi: السطر بيتقسّم مقاطع (عربي / لاتيني-أرقام / محايد)، بيتعكس ترتيب
    ///      المقاطع، وكل مقطع عربي بيتعكس حروفه — فالناتج ترتيب بصري جاهز للرسم
    ///      من الشمال لليمين (TMP بيتعامل معاه كـLTR عادي).
    ///   ٣) الأقواس والمزدوجات بتتعكس ( ﴾ ↔ ﴿، ( ↔ )، « ↔ » ).
    ///
    /// نتيجة: النص العربي بيطلع مقروء صح بدون أي بلاجن خارجي.
    /// </summary>
    public static class ArabicShaper
    {
        // ─────────── جدول الأشكال: معزول · نهائي · أولي · وسطي ───────────
        static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
        {
            { 'ء', new[] { '\uFE80', '\uFE80', '\0',     '\0'     } },
            { 'آ', new[] { '\uFE81', '\uFE82', '\0',     '\0'     } },
            { 'أ', new[] { '\uFE83', '\uFE84', '\0',     '\0'     } },
            { 'ؤ', new[] { '\uFE85', '\uFE86', '\0',     '\0'     } },
            { 'إ', new[] { '\uFE87', '\uFE88', '\0',     '\0'     } },
            { 'ئ', new[] { '\uFE89', '\uFE8A', '\uFE8B', '\uFE8C' } },
            { 'ا', new[] { '\uFE8D', '\uFE8E', '\0',     '\0'     } },
            { 'ب', new[] { '\uFE8F', '\uFE90', '\uFE91', '\uFE92' } },
            { 'ة', new[] { '\uFE93', '\uFE94', '\0',     '\0'     } },
            { 'ت', new[] { '\uFE95', '\uFE96', '\uFE97', '\uFE98' } },
            { 'ث', new[] { '\uFE99', '\uFE9A', '\uFE9B', '\uFE9C' } },
            { 'ج', new[] { '\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0' } },
            { 'ح', new[] { '\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4' } },
            { 'خ', new[] { '\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8' } },
            { 'د', new[] { '\uFEA9', '\uFEAA', '\0',     '\0'     } },
            { 'ذ', new[] { '\uFEAB', '\uFEAC', '\0',     '\0'     } },
            { 'ر', new[] { '\uFEAD', '\uFEAE', '\0',     '\0'     } },
            { 'ز', new[] { '\uFEAF', '\uFEB0', '\0',     '\0'     } },
            { 'س', new[] { '\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4' } },
            { 'ش', new[] { '\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8' } },
            { 'ص', new[] { '\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC' } },
            { 'ض', new[] { '\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0' } },
            { 'ط', new[] { '\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4' } },
            { 'ظ', new[] { '\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8' } },
            { 'ع', new[] { '\uFEC9', '\uFECA', '\uFECB', '\uFECC' } },
            { 'غ', new[] { '\uFECD', '\uFECE', '\uFECF', '\uFED0' } },
            { 'ف', new[] { '\uFED1', '\uFED2', '\uFED3', '\uFED4' } },
            { 'ق', new[] { '\uFED5', '\uFED6', '\uFED7', '\uFED8' } },
            { 'ك', new[] { '\uFED9', '\uFEDA', '\uFEDB', '\uFEDC' } },
            { 'ل', new[] { '\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0' } },
            { 'م', new[] { '\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4' } },
            { 'ن', new[] { '\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8' } },
            { 'ه', new[] { '\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC' } },
            { 'و', new[] { '\uFEED', '\uFEEE', '\0',     '\0'     } },
            { 'ى', new[] { '\uFEEF', '\uFEF0', '\0',     '\0'     } },
            { 'ي', new[] { '\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4' } },
        };

        // ليجاتشر لام + ألف: [معزول, نهائي]
        static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
        {
            { 'آ', new[] { '\uFEF5', '\uFEF6' } },
            { 'أ', new[] { '\uFEF7', '\uFEF8' } },
            { 'إ', new[] { '\uFEF9', '\uFEFA' } },
            { 'ا', new[] { '\uFEFB', '\uFEFC' } },
        };

        /// <summary>حروف بتوصل باللي قبلها وما بتوصلش باللي بعدها.</summary>
        static bool JoinsPreviousOnly(char c) => "اآأإدذرزوؤةىء".IndexOf(c) >= 0;

        /// <summary>حروف بتوصل من الجهتين.</summary>
        static bool JoinsBothSides(char c) =>
            Forms.ContainsKey(c) && !JoinsPreviousOnly(c) && c != 'ء';

        /// <summary>حركات/تشكيل — شفافة في حساب الوصل.</summary>
        static bool IsTransparent(char c) =>
            (c >= '\u064B' && c <= '\u0655') || c == '\u0670' || c == '\u0640' ||
            c == '\u0656' || c == '\u0657' || c == '\u0658';

        static bool IsArabicLetter(char c) => Forms.ContainsKey(c) || LamAlef.ContainsKey(c);

        // ────────────────────────── ١) التشكيل ──────────────────────────

        public static string Shape(string logical)
        {
            if (string.IsNullOrEmpty(logical)) return logical;

            var sb = new StringBuilder(logical.Length + 8);

            for (int i = 0; i < logical.Length; i++)
            {
                char c = logical[i];

                // لام + ألف ⇒ ليجاتشر
                if (c == 'ل')
                {
                    int j = NextMeaningful(logical, i + 1);
                    if (j >= 0 && LamAlef.ContainsKey(logical[j]))
                    {
                        bool prevJoins = PrevJoinsForward(logical, i);
                        var lig = LamAlef[logical[j]];
                        sb.Append(prevJoins ? lig[1] : lig[0]);
                        i = j;                       // نتخطى الألف
                        continue;
                    }
                }

                if (!Forms.TryGetValue(c, out var f))
                {
                    sb.Append(c);
                    continue;
                }

                bool joinPrev = PrevJoinsForward(logical, i);
                bool joinNext = NextJoinsBackward(logical, i);

                char shaped;
                if (joinPrev && joinNext && f[2] != '\0') shaped = f[2];        // وسطي
                else if (joinPrev && joinNext)            shaped = f[0];        // مالوش وسط ⇒ معزول
                else if (joinPrev && f[1] != '\0')        shaped = f[1];        // نهائي
                else if (joinNext && f[2] != '\0')        shaped = f[2];        // أولي
                else                                      shaped = f[0];        // معزول

                sb.Append(shaped);
            }

            return sb.ToString();
        }

        static int NextMeaningful(string s, int from)
        {
            for (int i = from; i < s.Length; i++)
                if (!IsTransparent(s[i])) return i;
            return -1;
        }

        static int PrevMeaningful(string s, int from)
        {
            for (int i = from; i >= 0; i--)
                if (!IsTransparent(s[i])) return i;
            return -1;
        }

        /// <summary>هل الحرف اللي قبل يوصل للأمام (يعني الحرف الحالي هيبقى متصل من قبله)؟</summary>
        static bool PrevJoinsForward(string s, int index)
        {
            int p = PrevMeaningful(s, index - 1);
            if (p < 0) return false;

            char prev = s[p];
            if (prev == 'ل') { /* لام دايماً بتوصل للأمام */ }

            // اللام قبل ألف اتحولت ليجاتشر في مرحلة سابقة، فمش هنشوفها هنا.
            return IsArabicLetter(prev) && !JoinsPreviousOnly(prev);
        }

        /// <summary>هل الحرف اللي بعد يقبل الوصل من الجهة اللي قبله؟</summary>
        static bool NextJoinsBackward(string s, int index)
        {
            int n = NextMeaningful(s, index + 1);
            if (n < 0) return false;

            char next = s[n];
            if (next == 'ل') return true;
            return IsArabicLetter(next) && next != 'ء';
        }

        // ────────────────────────── ٢) الترتيب البصري ──────────────────────────

        enum SegKind { Arabic, Ltr, Neutral }

        static SegKind Classify(char c)
        {
            // الأرقام (اللاتينية والعربية-الهندية) بتترسم من الشمال لليمين وترتيبها
            // جوه الرقم نفسه ما بيتعكسش — ده نص قاعدة Bidi للـEN/AN.
            if (c >= '0' && c <= '9') return SegKind.Ltr;
            if (c >= '\u0660' && c <= '\u0669') return SegKind.Ltr;   // ٠١٢٣٤٥٦٧٨٩
            if (c >= '\u06F0' && c <= '\u06F9') return SegKind.Ltr;   // ۰۱۲۳۴۵۶۷۸۹

            // حروف عربية وأشكال العرض
            if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\u0750' && c <= '\u077F') ||
                (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'))
                return SegKind.Arabic;

            if (char.IsLetterOrDigit(c) && c < '\u0600') return SegKind.Ltr;
            if (c >= '0' && c <= '9') return SegKind.Ltr;

            return SegKind.Neutral;
        }

        /// <summary>يرجّع النص جاهز للرسم (ترتيب بصري + أشكال).</summary>
        public static string Fix(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            if (!HasArabic(input)) return input;      // إنجليزي: مفيش أي تغيير

            var shaped = Shape(input);
            var lines = shaped.Split('\n');
            var outSb = new StringBuilder(shaped.Length + 8);

            for (int li = 0; li < lines.Length; li++)
            {
                if (li > 0) outSb.Append('\n');
                outSb.Append(VisualOrderLine(lines[li]));
            }

            return outSb.ToString();
        }

        static string VisualOrderLine(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;

            var segs = new List<(SegKind kind, string text)>();
            var cur = new StringBuilder();
            SegKind curKind = Classify(line[0]);

            for (int i = 0; i < line.Length; i++)
            {
                var k = Classify(line[i]);

                // المحايد بين حرفين من نفس النوع بياخد نوعهم
                if (k == SegKind.Neutral && curKind != SegKind.Neutral)
                    k = curKind;

                if (k != curKind && cur.Length > 0)
                {
                    segs.Add((curKind, cur.ToString()));
                    cur.Clear();
                    curKind = k;
                }
                else if (cur.Length == 0)
                {
                    curKind = k;
                }

                cur.Append(line[i]);
            }
            if (cur.Length > 0) segs.Add((curKind, cur.ToString()));

            var sb = new StringBuilder(line.Length + 8);
            for (int s = segs.Count - 1; s >= 0; s--)          // ترتيب المقاطع معكوس (أساس السطر عربي)
            {
                var seg = segs[s];
                if (seg.kind == SegKind.Arabic)
                {
                    for (int i = seg.text.Length - 1; i >= 0; i--)   // الحروف معكوسة
                        sb.Append(seg.text[i]);
                }
                else
                {
                    sb.Append(seg.text);                             // لاتيني/أرقام بترتيبها
                }
            }

            // ملاحظة: مش بنعكس الأقواس/المزدوجات (Mirror). لأن قلب ترتيب المقاطع
            // أصلاً بينقل علامة الفتح « للطرف اليمين وعلامة القفل » للطرف الشمال،
            // وده بالظبط الشكل الصحيح للنص العربي. عكسها كمان كان بيطلّعها مقلوبة.
            return CollapseSpaces(sb.ToString());
        }

        static string CollapseSpaces(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            char prev = '\0';
            foreach (var c in s)
            {
                if (c == ' ' && prev == ' ') continue;
                sb.Append(c);
                prev = c;
            }
            return sb.ToString();
        }

        static string Mirror(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '(': sb.Append(')'); break;
                    case ')': sb.Append('('); break;
                    case '[': sb.Append(']'); break;
                    case ']': sb.Append('['); break;
                    case '{': sb.Append('}'); break;
                    case '}': sb.Append('{'); break;
                    case '<': sb.Append('>'); break;
                    case '>': sb.Append('<'); break;
                    case '«': sb.Append('»'); break;
                    case '»': sb.Append('«'); break;
                    default:  sb.Append(c);  break;
                }
            }
            return sb.ToString();
        }

        static bool HasArabic(string s)
        {
            foreach (var c in s)
                if ((c >= '\u0600' && c <= '\u06FF') || (c >= '\uFB50' && c <= '\uFEFF'))
                    return true;
            return false;
        }

        /// <summary>هل النص اتشكّل/اتظبط خلاص؟ (مهم عشان ما نطبقش عليه مرتين)</summary>
        public static bool IsAlreadyVisual(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var c in s)
                if (c >= '\uFE70' && c <= '\uFEFF') return true;   // فيه أشكال عرض ⇒ اتشكّل
            return false;
        }
    }
}
