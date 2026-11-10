using System;
using System.Collections.Generic;
using System.Text;

namespace TLC.Cinematics
{
    /// <summary>أشكال الفم الستة اللي بنستخدمها للـ Lip Sync.</summary>
    public enum Viseme
    {
        Rest = 0,   // ساكت
        Open = 1,   // أ / فتح واسع
        Narrow = 2, // إ / ضيق
        Round = 3,  // و / مضموم
        Wide = 4,   // س / عريض
        Closed = 5  // م / ب / مغلق
    }

    public struct VisemeKey
    {
        public float time;      // ثانية
        public Viseme viseme;
        public float weight;    // 0..1

        public VisemeKey(float t, Viseme v, float w)
        {
            time = t; viseme = v; weight = w;
        }
    }

    /// <summary>
    /// يولّد جدول حركة فم من نص السطر ومدته.
    /// لما تيجي تسجيلات صوتية حقيقية، بيتحسب نفس الجدول من التوقيت الفعلي
    /// — يعني النظام جاهز ومش مربوط بغياب الصوت.
    /// </summary>
    public static class LipSyncTimeline
    {
        const float MinHold = 0.055f;    // أقصر مدة شكل

        /// <summary>حرف → شكل الفم. مبني على المخارج العربية والإنجليزية.</summary>
        public static Viseme For(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'م': case 'ب': case 'ف': case 'm': case 'b': case 'p':
                    return Viseme.Closed;
                case 'و': case 'ض': case 'o': case 'u': case 'w':
                    return Viseme.Round;
                case 'ي': case 'ى': case 'ك': case 'ج': case 'e': case 'i': case 'y':
                    return Viseme.Narrow;
                case 'ا': case 'أ': case 'إ': case 'آ': case 'ه': case 'ة': case 'ع': case 'ح':
                case 'a': case 'h':
                    return Viseme.Open;
                case 'س': case 'ص': case 'ز': case 'ش': case 'ث': case 'ذ': case 'ظ':
                case 's': case 'z': case 'c':
                    return Viseme.Wide;
                case 'ل': case 'ن': case 'ر': case 'ت': case 'د': case 'ط': case 'ق': case 'خ':
                case 'غ': case 'ء':
                case 'l': case 'n': case 'r': case 't': case 'd': case 'g': case 'q':
                    return Viseme.Open;
                default:
                    return Viseme.Rest;
            }
        }

        static bool IsLetter(char c) => char.IsLetter(c);

        /// <summary>
        /// يبني الجدول: شكل لكل حرف منطوق، موزّع على مدة السطر،
        /// مع إغلاق الفم عند علامات الترقيم وفواصل الكلام.
        /// </summary>
        public static List<VisemeKey> Build(string text, float duration)
        {
            var keys = new List<VisemeKey>();
            if (string.IsNullOrEmpty(text) || duration <= 0f)
            {
                keys.Add(new VisemeKey(0f, Viseme.Rest, 0f));
                return keys;
            }

            var letters = new List<char>();
            var pauses = new HashSet<int>();     // مؤشرات حروف قبلها وقفة
            var sb = new StringBuilder();

            foreach (char raw in text)
            {
                char c = raw;
                if (IsLetter(c))
                {
                    letters.Add(c);
                    sb.Append(c);
                }
                else if (c == '،' || c == ',' || c == '.' || c == '؟' || c == '!' ||
                         c == '…' || c == ':' || c == '؛' || c == ' ' || c == '\n')
                {
                    if (letters.Count > 0) pauses.Add(letters.Count);
                }
            }

            if (letters.Count == 0)
            {
                keys.Add(new VisemeKey(0f, Viseme.Rest, 0f));
                return keys;
            }

            // كل حرف بياخد شريحة زمنية؛ علامات الترقيم بتاخد شريحة أطول (وقفة)
            float unit = Mathf_Max(MinHold, duration / (letters.Count + pauses.Count * 1.6f));

            float t = 0f;
            for (int i = 0; i < letters.Count; i++)
            {
                if (pauses.Contains(i))
                {
                    keys.Add(new VisemeKey(t, Viseme.Rest, 0f));
                    t += unit * 1.6f;
                }

                var v = For(letters[i]);
                float weight = v == Viseme.Rest ? 0f : 0.75f;

                // تنويع بسيط يمنع الحركة الميكانيكية
                weight *= 0.85f + 0.15f * ((i * 37) % 11) / 10f;

                keys.Add(new VisemeKey(t, v, weight));
                t += unit;
            }

            keys.Add(new VisemeKey(t + unit, Viseme.Rest, 0f));
            return keys;
        }

        /// <summary>الشكل الغالب في لحظة زمنية معيّنة (مع تخميد بين المفاتيح).</summary>
        public static VisemeKey Sample(List<VisemeKey> keys, float time)
        {
            if (keys == null || keys.Count == 0)
                return new VisemeKey(time, Viseme.Rest, 0f);

            if (time <= keys[0].time) return keys[0];
            for (int i = 1; i < keys.Count; i++)
            {
                if (time <= keys[i].time)
                {
                    var a = keys[i - 1];
                    var b = keys[i];
                    float span = b.time - a.time;
                    float k = span <= 1e-5f ? 1f : (time - a.time) / span;
                    // أول نص الشريحة بنفس الشكل، وآخرها بنخفف — يبان كلام مش نبض
                    if (k > 0.72f) return new VisemeKey(time, b.viseme, b.weight * (1f - k) / 0.28f);
                    return a;
                }
            }
            return keys[keys.Count - 1];
        }

        static float Mathf_Max(float a, float b) => a > b ? a : b;
    }
}
