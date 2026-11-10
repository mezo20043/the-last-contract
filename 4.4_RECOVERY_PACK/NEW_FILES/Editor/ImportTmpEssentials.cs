// 4.4 RECOVERY — ImportTmpEssentials.cs — **مُعاد حرفيًا** (بما فيه انتظار الاستيراد غير المتزامن)
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// موارد TMP الأساسية (خط LiberationSans SDF + الإعدادات) لازم تكون في المشروع،
    /// وإلا كل TextMeshProUGUI بيفضل من غير خط والنص ما بيترسمش خالص.
    /// </summary>
    public static class ImportTmpEssentials
    {
        const string AssetsRoot = "Assets/TextMesh Pro";

        [MenuItem("TLC/0. استيراد موارد TMP الأساسية")]
        public static void Run()
        {
            if (Directory.Exists(Path.Combine(AssetsRoot, "Resources/Fonts & Materials")))
            {
                Debug.Log("[TLC/TMP] موارد TMP موجودة بالفعل — مفيش استيراد.");
                EditorApplication.Exit(0);
                return;
            }

            string pkg = FindPackage();
            if (pkg == null)
            {
                Debug.LogError("[TLC/TMP] ملف 'TMP Essential Resources.unitypackage' مش موجود في PackageCache.");
                EditorApplication.Exit(2);
                return;
            }

            Debug.Log("[TLC/TMP] بستورد: " + pkg);

            int waited = 0;
            EditorApplication.CallbackFunction tick = null;
            tick = () =>
            {
                bool fonts = Directory.Exists(Path.Combine(AssetsRoot, "Resources/Fonts & Materials"))
                             || Directory.Exists(Path.Combine(AssetsRoot, "Fonts"));
                bool settings = File.Exists(Path.Combine(AssetsRoot, "Resources/TMP Settings.asset"));

                if ((fonts && settings) || ++waited > 600)
                {
                    EditorApplication.update -= tick;
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"[TLC/TMP] بعد الاستيراد: خطوط={fonts} · إعدادات={settings} · فريمات={waited}");
                    EditorApplication.Exit(fonts && settings ? 0 : 3);
                }
            };

            EditorApplication.update += tick;
            AssetDatabase.ImportPackage(pkg, false);   // غير متزامن
        }

        static string FindPackage()
        {
            string cache = Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache");
            if (!Directory.Exists(cache)) return null;

            foreach (var dir in Directory.GetDirectories(cache, "com.unity.ugui*"))
            {
                string p = Path.Combine(dir, "Package Resources", "TMP Essential Resources.unitypackage");
                if (File.Exists(p)) return Path.GetFullPath(p);
            }
            return null;
        }
    }
}
