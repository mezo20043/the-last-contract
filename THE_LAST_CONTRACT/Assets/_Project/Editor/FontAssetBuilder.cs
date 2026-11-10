using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;

namespace TLC.EditorTools
{
    /// <summary>
    /// يبني Font Asset جاهز من خط Amiri وقت التحرير ويحفظه في Resources.
    ///
    /// ليه؟ توليد الخط وقت التشغيل (`TMP_FontAsset.CreateFontAsset`) كان بيرمي
    /// ArgumentNullException لأن إعدادات TMP الافتراضية مش مكتملة، فالواجهة كانت
    /// بتترسم **بأزرار فاضية بدون نص** (اللقطة 01_main_menu.png الأولى كانت كده).
    /// الحل: نبني الأطلس مرة واحدة في المحرر، والتشغيل بيحمّله كأصل جاهز.
    /// </summary>
    public static class FontAssetBuilder
    {
        const string TtfPath     = "Assets/_Project/Resources/Fonts/Amiri-Regular.ttf";
        const string TtfBoldPath = "Assets/_Project/Resources/Fonts/Amiri-Bold.ttf";
        const string OutRegular  = "Assets/_Project/Resources/Fonts/Amiri SDF.asset";
        const string OutBold     = "Assets/_Project/Resources/Fonts/Amiri SDF Bold.asset";

        [MenuItem("TLC/بناء خط TMP عربي")]
        public static void Build()
        {
            var regular = BuildOne(TtfPath, OutRegular, "Amiri SDF");
            if (regular != null) BuildOne(TtfBoldPath, OutBold, "Amiri SDF Bold");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TLC/Font] ✅ خط TMP العربي جاهز: " + (regular != null ? "نجح" : "فشل"));
        }

        static TMP_FontAsset BuildOne(string ttfPath, string outPath, string name)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null) { Debug.LogWarning("[TLC/Font] الخط مش موجود: " + ttfPath); return null; }

            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
            if (existing != null) { Debug.Log("[TLC/Font] موجود بالفعل: " + outPath); return existing; }

            TMP_FontAsset fa = null;
            try
            {
                fa = TMP_FontAsset.CreateFontAsset(
                    font,
                    samplingPointSize: 90,
                    atlasPadding: 9,
                    renderMode: GlyphRenderMode.SDFAA,
                    atlasWidth: 2048,
                    atlasHeight: 2048,
                    atlasPopulationMode: AtlasPopulationMode.Dynamic,
                    enableMultiAtlasSupport: true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC/Font] ⚠️ TMP_FontAsset.CreateFontAsset فشل: " + e.GetType().Name + " — " + e.Message);
                return null;
            }

            if (fa == null) { Debug.LogWarning("[TLC/Font] الناتج null: " + outPath); return null; }

            fa.name = name;
            AssetDatabase.CreateAsset(fa, outPath);

            // الأطلس + المادة لازم يتحفظوا كأصول كمان
            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    var tex = fa.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = name + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(tex, fa);
                }
            }
            if (fa.material != null)
            {
                fa.material.name = name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }

            EditorUtility.SetDirty(fa);
            Debug.Log("[TLC/Font] اتعمل: " + outPath);
            return fa;
        }
    }
}
