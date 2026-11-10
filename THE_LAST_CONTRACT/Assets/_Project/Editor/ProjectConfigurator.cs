#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace TLC.EditorTools
{
    /// <summary>
    /// يظبّط إعدادات المشروع كلها لنقرة واحدة — كل حاجة محتاجة عشان APK أندرويد شغّال.
    /// من غير ده، هتقعد ساعة تدوّر على "ليه الأزرار مش بتشتغل" و"ليه البناء فشل".
    /// </summary>
    public static class ProjectConfigurator
    {
        public const string BundleId = "com.thelastcontract.game";
        public const string ProductName = "The Last Contract";

        [MenuItem("TLC/0 · إعداد المشروع للأندرويد (افعلها الأول)", priority = 0)]
        public static void ApplyAll()
        {
            ApplyIdentification();
            ApplyRendering();
            ApplyAndroid();
            ApplyInputHandling();
            ApplyEditorSettings();

            AssetDatabase.SaveAssets();
            Debug.Log("✅ [TLC] المشروع اتظبّط للأندرويد. الخطوة اللي بعدها: TLC → 6 · بناء APK");
        }

        // ────────────────────────── الهوية ──────────────────────────

        static void ApplyIdentification()
        {
            PlayerSettings.companyName = "TLC Studio";
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = "0.1.0";

            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleId);
            PlayerSettings.Android.bundleVersionCode = 1;

            Debug.Log($"[TLC] Bundle ID: {BundleId}");
        }

        // ────────────────────────── الرندرة ──────────────────────────

        static void ApplyRendering()
        {
            // Linear ضروري للواقعية — Gamma بيقتل الـ PBR
            PlayerSettings.colorSpace = ColorSpace.Linear;

            // الاتجاهين مدعومين: الوضع الأساسي أفقي (منظور شخص ثالث أوسع)
            // والبورتريه مدعوم بتخطيط مختلف — OrientationWatcher بيبدّل التخطيط
            // وبيوسّع الكاميرا رأسياً عشان المجال الأفقي يفضل ثابت.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                UnityEngine.Rendering.GraphicsDeviceType.Vulkan,
                UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3
            });
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);

            Debug.Log("[TLC] Linear color space + Vulkan/GLES3 + دوران تلقائي (رأسي/أفقي)");
        }

        // ────────────────────────── أندرويد ──────────────────────────

        static void ApplyAndroid()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);

            // Low مش Medium — الـ stripping بيقصّ كود مبعوت في الـ reflection
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);

            PlayerSettings.Android.useCustomKeystore = false;   // بناء تجريبي موقّع بمفتاح debug

            Debug.Log("[TLC] IL2CPP · ARM64 · minSdk 26 · stripping Low");
        }

        // ────────────────────────── الإدخال ──────────────────────────

        static void ApplyInputHandling()
        {
            // 0 = Input Manager (Old) · 1 = Input System (New) · 2 = Both
            // الكود بيستخدم Input القديم → لازم Both
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[TLC] مش لاقي ProjectSettings.asset — ظبّط Active Input Handling يدوياً على Both.");
                return;
            }

            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");

            if (prop == null)
            {
                Debug.LogWarning("[TLC] الخاصية activeInputHandler مش موجودة — ظبّطها يدوياً على Both.");
                return;
            }

            if (prop.intValue != 2)
            {
                prop.intValue = 2;
                so.ApplyModifiedProperties();
                Debug.Log("[TLC] Active Input Handling = Both (مطلوب عشان الكيبورد واللمس يشتغلوا مع بعض).");
            }
            else
            {
                Debug.Log("[TLC] Active Input Handling = Both ✓");
            }
        }

        // ────────────────────────── المحرر ──────────────────────────

        static void ApplyEditorSettings()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;
            Debug.Log("[TLC] Asset Serialization = Force Text (مهم لـ Git)");
        }

        // ────────────────────────── فحص ──────────────────────────

        [MenuItem("TLC/7 · فحص جاهزية البناء", priority = 70)]
        public static void Audit()
        {
            Debug.Log("──────── فحص جاهزية بناء THE LAST CONTRACT ────────");
            Debug.Log($"Unity            : {Application.unityVersion}");
            Debug.Log($"Color Space      : {PlayerSettings.colorSpace}  (المطلوب Linear)");
            Debug.Log($"Bundle ID        : {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}");
            Debug.Log($"Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
            Debug.Log($"Architectures    : {PlayerSettings.Android.targetArchitectures}");
            Debug.Log($"Min SDK          : {PlayerSettings.Android.minSdkVersion}");
            Debug.Log($"Target           : {EditorUserBuildSettings.activeBuildTarget}");
            Debug.Log($"Scenes in build  : {EditorBuildSettings.scenes.Length}");

            var story = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/_Project/Resources/Story/chapter01.json");
            Debug.Log(story != null ? "✓ ملف القصة موجود" : "✗ ملف القصة مش موجود!");

            var urpAsset = GraphicsSettings.defaultRenderPipeline;
            Debug.Log(urpAsset != null
                ? $"✓ SRP: {urpAsset.name}"
                : "✗ مفيش Render Pipeline — المشروع مش URP!");

            Debug.Log("──────── خلص الفحص ────────");
        }
    }
}
#endif
