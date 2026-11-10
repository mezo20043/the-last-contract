#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLC.Bootstrap;

namespace TLC.EditorTools
{
    /// <summary>
    /// أدوات المشاهد: إنشاء/فتح Main و Play، وتأكيد إنهم في Build Settings.
    ///
    /// ⚠️ كان هنا باج: الثابت <c>ScenePath</c> كان بيشاور على
    /// <c>Scenes/Chapter01.unity</c> — مشهد مش موجود في المشروع.
    /// يعني أول فتح للمحرر كان بيعمل مشهد جديد فاضي اسمه Chapter01
    /// ويحطه في Build Settings، والمشهد الحقيقي (Main.unity) يفضل برة القائمة.
    /// اتصلّح: الأسماء بقت من SceneFlow، والمشهدين الاتنين بيتأكدوا.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectBootstrapper
    {
        const string ScenesFolder = "Assets/_Project/Scenes";
        const string MainPath = ScenesFolder + "/Main.unity";
        const string PlayPath = ScenesFolder + "/Play.unity";

        static ProjectBootstrapper()
        {
            EditorApplication.delayCall += EnsureScenes;
        }

        // ─────────────────────── المشاهد ───────────────────────

        [MenuItem("TLC/1 · إنشاء/تأكيد مشاهد المشروع", priority = 1)]
        public static void EnsureScenes()
        {
            Directory.CreateDirectory(ScenesFolder);

            bool createdMain = false;

            if (!File.Exists(MainPath))
            {
                CreateMainScene();
                createdMain = true;
            }

            if (!File.Exists(PlayPath))
            {
                PlaySceneBuilder.BuildPlayScene();
            }

            EnsureInBuildSettings();
            AssetDatabase.Refresh();

            if (createdMain)
                Debug.Log("[TLC] مشهد Main اتعمل — ده مشهد القائمة بس.");
        }

        static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrapGo = new GameObject("GAME_BOOTSTRAP");
            var bootstrap = bootstrapGo.AddComponent<GameBootstrap>();

            // مشهد القائمة: واجهة بس — العالم في مشهد Play
            var so = new SerializedObject(bootstrap);
            so.FindProperty("menuOnly").boolValue = true;
            so.FindProperty("showTitleMenu").boolValue = true;
            so.FindProperty("buildMobileControls").boolValue = false;
            so.FindProperty("playSceneName").stringValue = SceneFlow.PlayScene;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainPath);
        }

        public static void EnsureInBuildSettings()
        {
            var wanted = new[]
            {
                new EditorBuildSettingsScene(MainPath, true),
                new EditorBuildSettingsScene(PlayPath, true)
            };

            var existing = EditorBuildSettings.scenes;
            bool same = existing != null && existing.Length == wanted.Length;
            if (same)
            {
                for (int i = 0; i < wanted.Length; i++)
                    if (existing[i].path != wanted[i].path || !existing[i].enabled) { same = false; break; }
            }

            if (same) return;

            EditorBuildSettings.scenes = wanted;
            Debug.Log("[TLC] Build Settings: Main (index 0) ثم Play (index 1).");
        }

        // ─────────────────────── قوائم ───────────────────────

        [MenuItem("TLC/2 · فتح مشهد القائمة (Main)", priority = 2)]
        public static void OpenMain()
        {
            EnsureScenes();
            EditorSceneManager.OpenScene(MainPath);
        }

        [MenuItem("TLC/3 · فتح مشهد اللعب (Play)", priority = 3)]
        public static void OpenPlay()
        {
            EnsureScenes();
            EditorSceneManager.OpenScene(PlayPath);
        }

        [MenuItem("TLC/4 · مسح الحفظ (للاختبار)", priority = 4)]
        public static void ClearSaves()
        {
            string dir = Path.Combine(Application.persistentDataPath, "saves");
            if (!Directory.Exists(dir)) { Debug.Log("[TLC] مفيش حفظ أصلاً."); return; }

            foreach (var f in Directory.GetFiles(dir, "*.json")) File.Delete(f);
            Debug.Log("[TLC] اتمسح كل الحفظ.");
        }

        [MenuItem("TLC/10 · فحص إعدادات ومشاهد المشروع", priority = 10)]
        public static void Audit()
        {
            Debug.Log("──────── فحص TLC ────────");
            Debug.Log($"Unity: {Application.unityVersion}");
            Debug.Log($"Active Input Handling (لازم Both): {PlayerSettings.GetPropertyInt("activeInputHandler")}");
            Debug.Log($"Color Space: {PlayerSettings.colorSpace}");
            Debug.Log($"Target: {EditorUserBuildSettings.activeBuildTarget}");
            Debug.Log($"Scenes in build: {EditorBuildSettings.scenes.Length}");
            foreach (var s in EditorBuildSettings.scenes)
                Debug.Log($"  · {(s.enabled ? "✓" : "✗")} {s.path}");

            string story = "Assets/_Project/Resources/Story/chapter01.json";
            Debug.Log(File.Exists(story) ? "✓ ملف القصة موجود" : "✗ ملف القصة مش موجود!");
            Debug.Log("─────────────────────────");

            PlaySceneValidator.Validate(PlayPath);
        }

        [MenuItem("TLC/9 · إعادة توليد مشهد اللعب (Play)", priority = 9)]
        public static void RebuildPlayScene()
        {
            if (!EditorUtility.DisplayDialog("إعادة توليد Play.unity",
                    "المشهد الحالي هيتستبدل بالنسخة المولّدة. أي تعديل يدوي هيضيع. نكمل؟",
                    "أيوة", "إلغاء"))
                return;

            PlaySceneBuilder.BuildPlayScene();
            EnsureInBuildSettings();
        }
    }
}
#endif
