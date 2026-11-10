using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// ميزانية خامات أندرويد — استبعاد خامات `_rough` غير المستخدمة من **بناء أندرويد فقط**.
    ///
    /// ليه؟ `PropLibrary` بيبني مادة كل أصل من `diff` + `nor` بس — خامات `rough`
    /// مش بيتحمّلها أي سطر في المشروع، لكنها جوّه `Resources/` ⇒ بتتحمّل في الـAPK
    /// بلا فايدة (≈ ٧.٥ MB).
    ///
    /// المبدأ: **مفيش حذف ولا نقل لأي ملف**. الملفات الأصلية والقيم الأصلية محفوظة،
    /// والاستبعاد بيتم بـOverrides خاصة بمنصة أندرويد فقط:
    ///   Max Size = 4  ·  Compressed  ⇒  الخامة في الـAPK تبقى ≈ ٩٦ بايت بدل ≈ ٧٠٠ كيلوبايت.
    /// كل المنصات التانية (Windows/Standalone/Linux) **زي ما هي بالحرف**.
    ///
    /// كل عملية قابلة للتراجع بالكامل من المنيو، والأداة مابتشتغلش تلقائيًا.
    /// </summary>
    public static class AndroidTextureBudget
    {
        const string PropsRoot = "Assets/_Project/Resources/Props";
        const string Suffix    = "_rough";
        const int    MaxSize   = 4;          // 4×4 — أصغر مقاس يقبله Unity
        const string Android   = "Android";

        [MenuItem("TLC/أدوات/استبعاد _rough من أندرويد")]
        static void MenuApply() => Apply(verbose: true);

        [MenuItem("TLC/أدوات/استرجاع _rough (Windows/كل المنصات)")]
        static void MenuRevert() => Revert(verbose: true);

        [MenuItem("TLC/أدوات/تقرير ميزانية خامات أندرويد")]
        static void MenuReport() => Report();

        // ───────────────────────── التنفيذ ─────────────────────────

        public static void Apply(bool verbose)
        {
            var paths = FindRoughTextures();
            if (paths.Count == 0) { Debug.LogWarning("[TLC][Budget] مفيش خامات _rough اتلقت."); return; }

            int changed = 0;
            foreach (var path in paths)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                var s = imp.GetPlatformTextureSettings(Android);
                if (s.overridden && s.maxTextureSize <= MaxSize) continue;   // متطبَّق أصلاً

                s.name = Android;
                s.overridden = true;
                s.maxTextureSize = MaxSize;
                s.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
                s.textureCompression = TextureImporterCompression.Compressed;
                s.compressionQuality = 50;
                s.crunchedCompression = false;
                s.format = TextureImporterFormat.Automatic;
                imp.SetPlatformTextureSettings(s);
                imp.SaveAndReimport();
                changed++;
            }

            AssetDatabase.Refresh();
            if (verbose)
                Debug.Log($"[TLC][Budget] ✔ استبعاد _rough من أندرويد: {changed} خامة اتعدّلت من {paths.Count} · " +
                          $"Max Size={MaxSize}×{MaxSize} · الملفات الأصلية كما هي · المنصات التانية ما اتلمستش.");
        }

        public static void Revert(bool verbose)
        {
            var paths = FindRoughTextures();
            int changed = 0;
            foreach (var path in paths)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;

                var s = imp.GetPlatformTextureSettings(Android);
                if (!s.overridden) continue;                                  // مفيش Override أصلاً

                s.overridden = false;
                s.maxTextureSize = 2048;
                s.textureCompression = TextureImporterCompression.CompressedHQ;
                s.format = TextureImporterFormat.Automatic;
                imp.SetPlatformTextureSettings(s);
                imp.SaveAndReimport();
                changed++;
            }

            AssetDatabase.Refresh();
            if (verbose) Debug.Log($"[TLC][Budget] ✔ رجّعت {changed} خامة لإعداداتها الافتراضية على أندرويد.");
        }

        /// <summary>تقرير للقراءة فقط: مين متطبَّق عليه الاستبعاد ومين لأ.</summary>
        public static void Report()
        {
            var paths = FindRoughTextures();
            int applied = 0;
            foreach (var path in paths)
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                var s = imp?.GetPlatformTextureSettings(Android);
                bool on = s != null && s.overridden && s.maxTextureSize <= MaxSize;
                if (on) applied++;
                long bytes = 0;
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null) bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
                Debug.Log($"[TLC][Budget] {Path.GetFileName(path),-32} أندرويد={(on ? "مستبعَد (4×4)" : "كامل"),-14} " +
                          $"المقاس الحالي={tex?.width ?? 0}×{tex?.height ?? 0} · الذاكرة≈{bytes / 1024f:F1} KB");
            }
            Debug.Log($"[TLC][Budget] إجمالي: {paths.Count} خامة · مستبعَدة على أندرويد: {applied}");
        }

        static List<string> FindRoughTextures()
        {
            var list = new List<string>();
            if (!Directory.Exists(PropsRoot)) return list;
            foreach (var f in Directory.GetFiles(PropsRoot, "*" + Suffix + ".jpg", SearchOption.AllDirectories))
                list.Add(f.Replace('\\', '/'));
            list.Sort();
            return list;
        }
    }
}
