// 4.4 RECOVERY — BuildAmiriFontAsset.cs — **مُعاد حرفيًا**
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// بيبني «Amiri SDF» كأصل جاهز في Resources عشان الخط العربي ما يتولّدش
    /// وقت التشغيل كل مرة (التوليد الديناميكي بيبطّأ أول فريم وبيحتاج TMP Settings).
    /// </summary>
    public static class BuildAmiriFontAsset
    {
        const string FontPath = "Assets/_Project/Resources/Fonts/Amiri-Regular.ttf";
        const string AssetPath = "Assets/_Project/Resources/Fonts/Amiri SDF.asset";

        [MenuItem("TLC/1. بناء خط Amiri لـTMP")]
        public static void Run()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null)
            {
                Debug.Log("[TLC/Font] أصل Amiri SDF موجود بالفعل.");
                EditorApplication.Exit(0);
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[TLC/Font] ملف Amiri مش موجود: " + FontPath);
                EditorApplication.Exit(2);
                return;
            }

            TMP_FontAsset asset = null;
            try
            {
                asset = TMP_FontAsset.CreateFontAsset(
                    font, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, true);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[TLC/Font] توليد الخط فشل: " + e.Message);
                EditorApplication.Exit(3);
                return;
            }

            if (asset == null)
            {
                Debug.LogError("[TLC/Font] TMP رجّع أصل فاضي — TMP Settings ناقصة؟");
                EditorApplication.Exit(4);
                return;
            }

            asset.name = "Amiri SDF";
            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            AssetDatabase.CreateAsset(asset, AssetPath);

            if (asset.material != null)
            {
                asset.material.name = "Amiri SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            if (asset.atlasTextures != null)
                foreach (var t in asset.atlasTextures)
                    if (t != null && !AssetDatabase.Contains(t))
                    {
                        t.name = "Amiri SDF Atlas";
                        AssetDatabase.AddObjectToAsset(t, asset);
                    }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var check = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            Debug.Log($"[TLC/Font] تم بناء Amiri SDF · موجود={check != null} · أطلس={check?.atlasTextures?.Length}");
            EditorApplication.Exit(check != null ? 0 : 5);
        }
    }
}
