using System.Collections.Generic;
using UnityEngine;

namespace TLC.World
{
    /// <summary>
    /// مكتبة الشخصيات الواقعية: بتحمّل موديلات بشرية Humanoid من Resources.
    ///
    /// الأصول: نماذج بشرية ماسحة عالية التفاصيل (143k–427k رأس، 1.70–1.80 م)،
    /// مرجّعة بهيكل عظمي بشري، بترخيص CC0 (انظر ASSETS_LICENSES.md).
    /// </summary>
    public static class CharacterLibrary
    {
        public enum Cast
        {
            Adam = 0,      // آدم منصور — البطل
            Murad = 1,     // مراد — الرجل القديم
            Laila = 2,     // ليلى
            Yassin = 3,    // ياسين — الأخ
            Yusuf = 4      // يوسف — رجل الأعمال (المشهد ٨)
        }

        public class Entry
        {
            public Cast cast;
            public string key;            // اسم الملف في Resources
            public string displayName;
            public string role;
            public float height;          // متر — من الميش المتقاس
            public string modelFile;      // اسم ملف الـFBX جوه مجلد الشخصية
            public Color rimColor;        // لون الإضاءة المميزة للشخصية
        }

        public static readonly Entry[] Roster =
        {
            new Entry { cast = Cast.Adam,   key = "adam",   modelFile = "man-base-character-rigged",
                        displayName = "آدم منصور",
                        role = "البطل", height = 1.80f, rimColor = new Color(0.62f, 0.72f, 0.95f) },
            new Entry { cast = Cast.Murad,  key = "murad",  modelFile = "elderly-man-rigged",
                        displayName = "مراد",
                        role = "الرجل القديم", height = 1.75f, rimColor = new Color(0.85f, 0.70f, 0.45f) },
            new Entry { cast = Cast.Laila,  key = "laila",  modelFile = "woman-rigged",
                        displayName = "ليلى",
                        role = "الصحافية", height = 1.70f, rimColor = new Color(0.90f, 0.62f, 0.55f) },
            new Entry { cast = Cast.Yassin, key = "yassin", modelFile = "man-rigged",
                        displayName = "ياسين",
                        role = "الأخ", height = 1.80f, rimColor = new Color(0.55f, 0.80f, 0.65f) },
            // ملاحظة صريحة: مفيش موديل مخصّص ليوسف على القرص، فبنستخدم موديل
            // الرجل الكبير الموجود (murad/elderly-man-rigged) بإضاءة حواف مختلفة.
            // لو اتضاف موديل جديد في Resources/Characters/yusuf هياخده تلقائيًا من المسار التاني.
            new Entry { cast = Cast.Yusuf, key = "murad", modelFile = "elderly-man-rigged",
                        displayName = "يوسف",
                        role = "رجل الأعمال", height = 1.78f, rimColor = new Color(0.95f, 0.55f, 0.40f) },
        };

        static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, bool> Probed = new Dictionary<string, bool>();

        public static Entry Get(Cast cast)
        {
            foreach (var e in Roster) if (e.cast == cast) return e;
            return null;
        }

        /// <summary>يرجّع الـ Prefab أو null لو الأصل مش موجود — بيتعامل معاه بالبديل.</summary>
        public static GameObject Load(Cast cast)
        {
            var entry = Get(cast);
            return entry == null ? null : Load(entry.key);
        }

        public static GameObject Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var prefab = LoadModel(key);
            Cache[key] = prefab;

            if (prefab == null && (!Probed.TryGetValue(key, out bool warned) || !warned))
            {
                Probed[key] = true;
                Debug.LogWarning($"[TLC/Characters] موديل '{key}' مش موجود في Resources/Characters — هيستخدم البديل البدائي.");
            }
            return prefab;
        }

        /// <summary>
        /// تحميل الموديل الحقيقي. الترتيب:
        ///   ١) Resource/Characters/{key}/{modelFile}  ← المسار الفعلي للملفات
        ///   ٢) أي موديل موجود جوه مجلد الشخصية
        ///   ٣) Resource/Characters/{key} (لو حطيت prefab مباشرة)
        /// الأول كان بيقرا (٣) بس، والموديلات محطوطة في مجلدات ⇒ ماكانتش بتتحمّل
        /// وكان بيرجع للبديل البدائي (المكعبات). ده كان سبب ظهور آدم كمكعبات.
        /// </summary>
        static GameObject LoadModel(string key)
        {
            var entry = Get((Cast)System.Enum.Parse(typeof(Cast), Capitalize(key)));
            if (entry != null && !string.IsNullOrEmpty(entry.modelFile))
            {
                var direct = Resources.Load<GameObject>($"Characters/{key}/{entry.modelFile}");
                if (direct != null) return direct;
            }

            var all = Resources.LoadAll<GameObject>("Characters/" + key);
            if (all != null && all.Length > 0)
            {
                System.Array.Sort(all, (a, b) => b.name.Length.CompareTo(a.name.Length));
                return all[0];
            }

            return Resources.Load<GameObject>("Characters/" + key);
        }

        static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>هل الشخصيات الواقعية متوفرة فعلاً على القرص.</summary>
        public static int AvailableCount()
        {
            int n = 0;
            foreach (var e in Roster) if (Load(e.key) != null) n++;
            return n;
        }

        public static IEnumerable<Entry> All => Roster;

        public static void Clear() => Cache.Clear();
    }
}
