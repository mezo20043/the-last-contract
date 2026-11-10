using System.IO;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// استيراد موارد TextMeshPro الأساسية أوتوماتيك.
    ///
    /// ليه؟ `ArabicFont` بيبني `TMP_FontAsset` وقت التشغيل، وده بيعتمد على
    /// `TMP Settings` اللي بييجي مع «Import TMP Essential Resources».
    /// من غير الاستيراد ده، أول سطر واجهة بيرمي NullReferenceException
    /// (`TMP_Settings.get_clearDynamicDataOnBuild`) — وده اللي حصل فعلاً في أول تشغيل.
    /// </summary>
    [InitializeOnLoad]
    public static class TMPEssentials
    {
        const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        static TMPEssentials()
        {
            EditorApplication.delayCall += () => Ensure(false);
        }

        [MenuItem("TLC/استيراد موارد TMP")]
        public static void MenuItem() => Ensure(true);

        /// <summary>بيدوّر على «TMP Essential Resources.unitypackage» جوه حزمة ugui.</summary>
        static string FindPackageFile()
        {
            var guids = AssetDatabase.FindAssets("TMP Essential Resources", new[] {
                "Packages/com.unity.ugui", "Assets" });
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                if (path != null && path.EndsWith(".unitypackage")) return path;
            }

            // بحث مباشر على القرص (الحزمة المدمجة)
            var candidates = new[]
            {
                "Library/PackageCache/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(),
                    "Library/PackageCache/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage")
            };
            foreach (var c in candidates)
                if (System.IO.File.Exists(c)) return System.IO.Path.GetFullPath(c);
            return null;
        }

        public static void Ensure(bool verbose)
        {
            bool hasSettings = AssetDatabase.LoadAssetAtPath<Object>(SettingsPath) != null;
            bool hasShaders = System.IO.Directory.Exists("Assets/TextMesh Pro/Shaders");
            if (hasSettings && hasShaders)
            {
                if (verbose) Debug.Log("[TLC] موارد TMP كاملة (Settings + Shaders)");
                return;
            }
            if (hasSettings && !hasShaders)
            {
                Debug.Log("[TLC] TMP Settings موجود بس الشيدرز ناقصة — هستورد الحزمة");
                // نحذف الـSettings المؤقت عشان الاستيراد الرسمي يكتب فوقه
                AssetDatabase.DeleteAsset(SettingsPath);
                AssetDatabase.Refresh();
            }

            // المحاولة الرسمية: استيراد ملف الحزمة مباشرة.
            // (TMP_PackageResourceImporter بيرجع «نجاح» بدون ما يستورد فعليًا في وضع batchmode)
            try
            {
                string pkg = FindPackageFile();
                if (pkg != null)
                {
                    Debug.Log("[TLC] استيراد: " + pkg);
                    AssetDatabase.ImportPackage(pkg, false);
                    AssetDatabase.Refresh();
                }
                else Debug.LogWarning("[TLC] ملف TMP Essential Resources مش موجود على القرص");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC] استيراد TMP الرسمي فشل: " + e.Message);
            }

            if (AssetDatabase.LoadAssetAtPath<Object>(SettingsPath) != null)
            {
                Debug.Log("[TLC] ✅ موارد TMP الأساسية اتستوردت رسميًا");
                return;
            }

            // خطة بديلة مضمونة: ننشئ «TMP Settings» بأنفسنا.
            // TMP محتاج الملف ده عشان يشتغل — من غيره أي توليد خط بيرمي
            // NullReferenceException في TMP_Settings.get_clearDynamicDataOnBuild.
            try
            {
                Directory.CreateDirectory("Assets/TextMesh Pro/Resources");
                var settings = ScriptableObject.CreateInstance<TMPro.TMP_Settings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);

                // خط افتراضي أيضًا لو متاح من الحزمة
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[TLC] ✅ أنشأت TMP Settings احتياطيًا: " + SettingsPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC] ⚠️ تعذر إنشاء TMP Settings: " + e.Message);
            }
        }
    }
}
