using System.IO;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// إصلاح استيراد خرائط الـNormal — **أداة صريحة، مش أوتوماتيك**.
    ///
    /// المشكلة: خامات `Props/*_nor.jpg` مستوردة النوع `Default` و sRGB **مفعّل**،
    /// وده غلط تقنيًا لخريطة Normal: لازم `NormalMap` + sRGB مقفول.
    /// النتيجة الحالية: الإضاءة بتتحسب على أساس طبيعي مكسور ⇒ الأسطح بتبان مسطّحة/غامقة.
    ///
    /// ما تعمل الأداة: بتغيّر **إعدادات الاستيراد بس** — ملفات JPG نفسها ما بتتلمسش،
    /// ومفيش أي حذف. وكل عملية ليها تراجع.
    ///
    /// قبل الاعتماد: شوف المقارنة البصرية في `R1_8_NOR_BEFORE.png` / `R1_8_NOR_AFTER.png`.
    /// </summary>
    public static class NormalMapImportTool
    {
        const string PropsRoot = "Assets/_Project/Resources/Props";

        [MenuItem("TLC/أدوات/إصلاح استيراد خرائط الـNormal (تجريبي)")]
        static void MenuApply() => Apply(true);

        [MenuItem("TLC/أدوات/تراجع عن إصلاح خرائط الـNormal")]
        static void MenuRevert() => Revert(true);

        public static void Apply(bool verbose)
        {
            int n = 0;
            foreach (var path in FindNormalMaps())
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType == TextureImporterType.NormalMap && !imp.sRGBTexture) continue; // متطبَّق

                imp.textureType      = TextureImporterType.NormalMap;
                imp.sRGBTexture      = false;
                imp.maxTextureSize   = 512;    // القرار المعتمد R1.8.1: 512 = نص الذاكرة + جودة مطابقة بصريًا
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
                n++;
            }
            AssetDatabase.Refresh();
            if (verbose)
                Debug.Log($"[TLC][Normal] ✔ اتصلّح {n} خامة ⇒ NormalMap · sRGB=off · maxSize=512. " +
                          $"ملفات JPG الأصلية ما اتلمستش.");
        }

        public static void Revert(bool verbose)
        {
            int n = 0;
            foreach (var path in FindNormalMaps())
            {
                var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                if (imp.textureType == TextureImporterType.Default && imp.sRGBTexture) continue;

                imp.textureType = TextureImporterType.Default;
                imp.sRGBTexture = true;
                imp.SaveAndReimport();
                n++;
            }
            AssetDatabase.Refresh();
            if (verbose) Debug.Log($"[TLC][Normal] رجّعت {n} خامة لإعدادها الأصلي (Default + sRGB).");
        }

        static string[] FindNormalMaps()
        {
            if (!Directory.Exists(PropsRoot)) return new string[0];
            var list = Directory.GetFiles(PropsRoot, "*_nor.jpg", SearchOption.AllDirectories);
            for (int i = 0; i < list.Length; i++) list[i] = list[i].Replace('\\', '/');
            System.Array.Sort(list);
            return list;
        }
    }
}
