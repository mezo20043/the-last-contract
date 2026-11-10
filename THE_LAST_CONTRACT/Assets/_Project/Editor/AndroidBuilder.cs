#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// بناء APK / AAB للأندرويد من غير ما تلمس Build Settings.
    /// بيتأكد الأول إن المشهد موجود في البناء، ويظبّط الإعدادات، ويبني.
    ///
    /// من المنيو:  TLC → 6 · بناء APK
    /// من سطر الأوامر:
    ///   Unity -quit -batchmode -projectPath . -executeMethod TLC.EditorTools.AndroidBuilder.BuildApkCI
    /// </summary>
    public static class AndroidBuilder
    {
        const string OutputFolder = "Builds/Android";
        const string ApkName = "TheLastContract.apk";
        const string AabName = "TheLastContract.aab";

        [MenuItem("TLC/6 · بناء APK (Android)", priority = 60)]
        public static void BuildApk() => Build(BuildTarget.Android, false);

        [MenuItem("TLC/6b · بناء AAB لمتجر جوجل", priority = 61)]
        public static void BuildAab() => Build(BuildTarget.Android, true);

        static void Build(BuildTarget target, bool appBundle)
        {
            // 1) تأكد إن المشهد متسجّل في البناء
            if (EditorBuildSettings.scenes.Length == 0)
            {
                Debug.LogWarning("[TLC] مفيش مشاهد في Build Settings — بجرّب أعمل المشهد تلقائياً.");
                ProjectBootstrapper.EnsureScenes();
            }

            if (EditorBuildSettings.scenes.Length == 0)
            {
                Fail("مفيش مشهد في Build Settings. من المنيو: TLC → 1 · إنشاء مشهد الفصل الأول");
                return;
            }

            // 2) ظبّط الإعدادات
            ProjectConfigurator.ApplyAll();

            // 3) مخرج
            Directory.CreateDirectory(OutputFolder);
            string fileName = appBundle ? AabName : ApkName;
            string outputPath = Path.Combine(OutputFolder, fileName);

            EditorUserBuildSettings.buildAppBundle = appBundle;

            var scenes = new string[EditorBuildSettings.scenes.Length];
            for (int i = 0; i < scenes.Length; i++)
                scenes[i] = EditorBuildSettings.scenes[i].path;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };

            Debug.Log($"[TLC] جاري البناء → {outputPath}");
            Debug.Log($"[TLC] المشاهد: {string.Join(", ", scenes)}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ [TLC] البناء نجح — {outputPath} · " +
                          $"{summary.totalSize / (1024 * 1024)} ميجا · {summary.totalTime.TotalSeconds:0} ثانية");
                EditorUtility.RevealInFinder(outputPath);
            }
            else
            {
                Fail($"البناء فشل: {summary.result} · أخطاء: {summary.totalErrors} · تحذيرات: {summary.totalWarnings}");
            }
        }

        static void Fail(string message)
        {
            Debug.LogError($"[TLC] {message}");
        }

        // ── نقاط دخول لسطر الأوامر (CI) ──

        public static void BuildApkCI()
        {
            try
            {
                BuildApk();
                bool ok = File.Exists(Path.Combine(OutputFolder, ApkName));
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] استثناء في البناء: {e}");
                EditorApplication.Exit(2);
            }
        }

        public static void BuildAabCI()
        {
            try
            {
                BuildAab();
                bool ok = File.Exists(Path.Combine(OutputFolder, AabName));
                EditorApplication.Exit(ok ? 0 : 1);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TLC] استثناء في البناء: {e}");
                EditorApplication.Exit(2);
            }
        }
    }
}
#endif
