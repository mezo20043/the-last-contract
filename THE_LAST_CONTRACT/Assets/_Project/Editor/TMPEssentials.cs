using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// موارد TextMeshPro الأساسية — <b>فحص آمن + استيراد يدوي فقط</b>.
    ///
    /// ليه؟ `ArabicFont` بيبني `TMP_FontAsset` وقت التشغيل، وده بيعتمد على
    /// `TMP Settings` اللي بييجي مع «Import TMP Essential Resources».
    /// من غير الاستيراد ده، أول سطر واجهة بيرمي NullReferenceException
    /// (`TMP_Settings.get_clearDynamicDataOnBuild`) — وده اللي حصل فعلاً في أول تشغيل.
    ///
    /// ── سياسة الأمان (R1.7) ────────────────────────────────────────────
    /// • المسار الأوتوماتيكي (عند فتح المشروع) = <b>فحص وقراءة فقط</b>:
    ///   مفيش حذف، مفيش إنشاء، مفيش استبدال. لو فيه نقص بيظهر Warning واضح.
    /// • أي تعديل على الملفات بيحصل <b>بس</b> من منيو `TLC/` (إجراء صريح من المستخدم).
    /// • قبل أي استيراد، يتعمل نسخة احتياطية من `TMP Settings` الحالي،
    ///   ولو الاستيراد غيّر محتواه الأصلي بيتم استرجاعه كما هو (نفس الملف ونفس الـGUID).
     /// </summary>
    [InitializeOnLoad]
    public static class TMPEssentials
    {
        const string SettingsPath   = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        const string SettingsMeta   = "Assets/TextMesh Pro/Resources/TMP Settings.asset.meta";
        const string SettingsDir    = "Assets/TextMesh Pro/Resources";
        const string ShadersDir     = "Assets/TextMesh Pro/Shaders";
        const string BackupRoot     = "Library/TLCEditorBackups";
        const string SessionKey     = "TLC.TMPEssentials.Warned";

        static TMPEssentials()
        {
            // فحص فقط عند التحميل — ممنوع أي كتابة هنا
            EditorApplication.delayCall += AutoCheck;
        }

        // ───────────────────────── فحص (بدون أي تعديل) ─────────────────────────

        static bool HasSettings() => AssetDatabase.LoadAssetAtPath<Object>(SettingsPath) != null;
        static bool HasShaders()  => Directory.Exists(ShadersDir);
        static bool IsComplete()  => HasSettings() && HasShaders();

        /// <summary>فحص للقراءة فقط. بيتنادى تلقائيًا عند الفتح + من المنيو.</summary>
        public static void AutoCheck()
        {
            if (IsComplete())
            {
                SessionState.EraseString(SessionKey);
                return;
            }

            // مش بنكرر نفس التنبيه أكتر من مرة في الجلسة الواحدة
            string state = (HasSettings() ? "S" : "-") + (HasShaders() ? "H" : "-");
            if (SessionState.GetString(SessionKey, "") == state) return;
            SessionState.SetString(SessionKey, state);

            if (!HasSettings())
                Debug.LogWarning(
                    "[TLC][TMP] «TMP Settings» مش موجود.\n" +
                    "        المشروع محتاجه عشان `ArabicFont` يبني الخط — من غيره أول سطر واجهة بيرمي NullReferenceException.\n" +
                    "        الحل (يدوي): " + SettingsPath + " من الريبو، أو منيو TLC/استيراد موارد TMP.\n" +
                    "        ملاحظة: السكربت ده ما بينشئش ولا بيحذف أي ملف تلقائيًا.");

            if (!HasShaders())
                Debug.LogWarning(
                    "[TLC][TMP] الشيدرز ناقصة: " + ShadersDir + "\n" +
                    "        الـ«TMP Settings» الموجود سليم ومش هيتلمس.\n" +
                    "        لو محتاج الشيدرز: منيو TLC/استيراد موارد TMP (بياخد نسخة احتياطية أولًا ويحافظ على إعداداتك).");
        }

        // ───────────────────────── إجراء صريح من المنيو ─────────────────────────

        [MenuItem("TLC/فحص موارد TMP")]
        static void MenuCheck()
        {
            SessionState.EraseString(SessionKey);   // نعرض التنبيه دايمًا لما المستخدم يطلبه
            if (IsComplete())
                Debug.Log("[TLC][TMP] ✔ الموارد كاملة (Settings + Shaders) — مفيش أي إجراء مطلوب");
            else
                AutoCheck();
        }

        [MenuItem("TLC/استيراد موارد TMP (بيعدّل الملفات)")]
        static void MenuImport() => Ensure(true);

        /// <summary>
        /// المسار الصريح الوحيد اللي ممكن يعدّل ملفات. بيتنادى من المنيو
        /// ومن `MenuFlowCapture` (تشغيل يدوي). <b>مش بيتنادى أوتوماتيك.</b>
        /// </summary>
        public static void Ensure(bool verbose)
        {
            bool hadSettings = HasSettings();

            if (hadSettings && HasShaders())
            {
                if (verbose) Debug.Log("[TLC][TMP] الموارد كاملة (Settings + Shaders) — مفيش أي تعديل");
                return;
            }

            if (verbose)
                Debug.Log("[TLC][TMP] نقص: Settings=" + (hadSettings ? "موجود" : "مفقود") +
                          " · Shaders=" + (HasShaders() ? "موجودة" : "ناقصة") + " ⇒ محاولة إصلاح يدوية");

            // 1) نسخة احتياطية قبل أي استيراد — الإعدادات الحالية محفوظة في كل الأحوال
            string backupFile = null, originalHash = null;
            if (hadSettings)
            {
                originalHash = Hash(SettingsPath);
                backupFile = BackupSettings();
                if (backupFile != null)
                    Debug.Log("[TLC][TMP] نسخة احتياطية من إعداداتك الحالية: " + backupFile);
                else
                    Debug.LogWarning("[TLC][TMP] ⚠️ فشل أخذ نسخة احتياطية — الاستيراد هيتخطى لحماية الإعدادات");
            }

            // 2) الاستيراد الرسمي (حزمة ugui) — مش بنحذف أي حاجة قبلها
            if (backupFile != null || !hadSettings)
                TryImportPackage();
            else
                Debug.LogWarning("[TLC][TMP] الاستيراد اتخطى (مفيش نسخة احتياطية). الملفات زي ما هي.");

            // 3) لو الاستيراد استبدل إعداداتك الأصلية ⇒ نرجّعها كما هي (نفس الملف ونفس الـGUID)
            if (hadSettings && backupFile != null && HasSettings() && Hash(SettingsPath) != originalHash)
            {
                Debug.LogWarning("[TLC][TMP] الاستيراد حاول يستبدل «TMP Settings» — بنرجّع نسختك الأصلية");
                RestoreSettings(backupFile);
            }

            // 4) خطة بديلة — بس لو لسه مفيش Settings أصلاً (مسار صريح فقط)
            if (!HasSettings())
            {
                Debug.LogWarning("[TLC][TMP] مش لاقي حزمة TMP الرسمية ⇒ هينشئ «TMP Settings» افتراضي (إجراء صريح). " +
                                 "النسخة الأصلية (لو فيه) موجودة في: " + BackupRoot);
                CreateFallbackSettings();
            }

            // 5) تقرير الحالة
            if (IsComplete()) Debug.Log("[TLC][TMP] ✔ الموارد كاملة (Settings + Shaders)");
            else if (HasSettings())
                Debug.LogWarning("[TLC][TMP] «TMP Settings» موجود وسليم، والشيدرز لسه ناقصة (اللعبة بتشتغل من غيرها).");
            else
                Debug.LogWarning("[TLC][TMP] ⚠️ لسه ناقص — راجع المنيو يدويًا.");
        }

        // ───────────────────────── أدوات ─────────────────────────

        static void TryImportPackage()
        {
            try
            {
                string pkg = FindPackageFile();
                if (pkg == null)
                {
                    Debug.LogWarning("[TLC][TMP] ملف TMP Essential Resources مش موجود على القرص — استيراد يدوي من Package Manager.");
                    return;
                }
                Debug.Log("[TLC][TMP] استيراد: " + pkg);
                AssetDatabase.ImportPackage(pkg, false);
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC][TMP] استيراد TMP الرسمي فشل: " + e.Message);
            }
        }

        static string BackupSettings()
        {
            try
            {
                string dir = Path.Combine(BackupRoot, System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(dir);
                var dest = Path.Combine(dir, "TMP Settings.asset");
                File.Copy(SettingsPath, dest, true);
                if (File.Exists(SettingsMeta)) File.Copy(SettingsMeta, dest + ".meta", true);
                return dest;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC][TMP] تعذر عمل النسخة الاحتياطية: " + e.Message);
                return null;
            }
        }

        static void RestoreSettings(string backupFile)
        {
            try
            {
                File.Copy(backupFile, SettingsPath, true);
                if (File.Exists(backupFile + ".meta")) File.Copy(backupFile + ".meta", SettingsMeta, true);
                AssetDatabase.ImportAsset(SettingsPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[TLC][TMP] ✔ تم استرجاع «TMP Settings» الأصلي (بدون تغيير في المحتوى أو الـGUID)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC][TMP] تعذر الاسترجاع: " + e.Message + " — النسخة الاحتياطية في " + backupFile);
            }
        }

        static void CreateFallbackSettings()
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);
                var settings = ScriptableObject.CreateInstance<TMPro.TMP_Settings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[TLC][TMP] أنشأت «TMP Settings» افتراضي: " + SettingsPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TLC][TMP] تعذر إنشاء TMP Settings: " + e.Message);
            }
        }

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

            var candidates = new[]
            {
                "Library/PackageCache/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
                Path.Combine(Directory.GetCurrentDirectory(),
                    "Library/PackageCache/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage")
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return Path.GetFullPath(c);
            return null;
        }

        static string Hash(string path)
        {
            try
            {
                using (var sha = SHA256.Create())
                using (var fs = File.OpenRead(path))
                    return System.BitConverter.ToString(sha.ComputeHash(fs));
            }
            catch { return null; }
        }
    }
}
