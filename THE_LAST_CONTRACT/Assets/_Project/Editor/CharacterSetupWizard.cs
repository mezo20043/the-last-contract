#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// معالج إعداد الشخصية الواقعية.
    ///
    /// الهدف: تحويل موديل Humanoid جاهز (Mixamo مجاني، Character Creator 4، Reallusion)
    /// لشخصية قابلة للعب في المشروع — تلقائياً:
    ///   1. يظبّط الـ Rig على Humanoid ويعمل Avatar.
    ///   2. يبني Animator Controller (Idle / Walk / Run) بمشغّلات السرعة.
    ///   3. بيربطهم بملف التعريف المقصود.
    ///
    /// الخطوات عندك:
    ///   • نزّل من Mixamo: شخصية + أنيميشن Idle و Walk و Run (FBX for Unity).
    ///   • حطّهم في Assets/_Project/Art/Characters/.
    ///   • اختار ملف الشخصية من Project view.
    ///   • TLC → 5 · إعداد شخصية واقعية.
    ///   • ضيف CharacterModelSlot على ADAM_MANSOUR وحطّ الـ Prefab فيها.
    /// </summary>
    public static class CharacterSetupWizard
    {
        const string AnimationsFolder = "Assets/_Project/Animations";

        [MenuItem("TLC/5 · إعداد شخصية واقعية (Mixamo / CC4)", priority = 50)]
        public static void SetupSelectedCharacter()
        {
            var selected = Selection.activeObject;
            if (selected == null)
            {
                Debug.LogError("[TLC/Character] اختار ملف الموديل (FBX) من Project view الأول.");
                return;
            }

            string path = AssetDatabase.GetAssetPath(selected);
            if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"[TLC/Character] '{path}' مش ملف FBX.");
                return;
            }

            ConfigureAsHumanoid(path);
            var clips = FindClips(path);
            var controller = BuildController(path, clips);

            if (controller != null)
                Debug.Log($"✅ [TLC/Character] اتجهّز Animator Controller: " +
                          $"{AssetDatabase.GetAssetPath(controller)}\n" +
                          "دلوقتي: ضيف CharacterModelSlot على ADAM_MANSOUR واربط الموديل والكونترولر.");
        }

        // ────────────────────────── Rig ──────────────────────────

        static void ConfigureAsHumanoid(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning("[TLC/Character] مش قادر أوصل للـ ModelImporter.");
                return;
            }

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.importAnimation == false)
            {
                importer.importAnimation = true;
                changed = true;
            }

            // ضغط الحركة — مهم جداً لحجم الـ APK
            if (importer.animationCompression != ModelImporterAnimationCompression.Optimal)
            {
                importer.animationCompression = ModelImporterAnimationCompression.Optimal;
                changed = true;
            }

            if (importer.meshCompression != ModelImporterMeshCompression.Medium)
            {
                importer.meshCompression = ModelImporterMeshCompression.Medium;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[TLC/Character] Rig → Humanoid + ضغط الحركة: {path}");
            }
        }

        // ────────────────────────── الكليبات ──────────────────────────

        static List<AnimationClip> FindClips(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToList();
        }

        static AnimationClip Pick(List<AnimationClip> clips, params string[] keywords)
        {
            foreach (var clip in clips)
            {
                string n = clip.name.ToLowerInvariant();
                foreach (var k in keywords)
                    if (n.Contains(k)) return clip;
            }
            return clips.FirstOrDefault();
        }

        // ────────────────────────── Animator ──────────────────────────

        static AnimatorController BuildController(string modelPath, List<AnimationClip> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                Debug.LogWarning("[TLC/Character] مفيش أنيميشن كليبات في الملف ده. " +
                                 "نزّل أنيميشن من Mixamo واربطها في الـ Animator يدوياً.");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(AnimationsFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Animations");

            string modelName = System.IO.Path.GetFileNameWithoutExtension(modelPath)
                .Replace(" ", "_");
            string controllerPath = $"{AnimationsFolder}/{modelName}_Locomotion.controller";

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                Debug.Log($"[TLC/Character] الكونترولر موجود بالفعل: {controllerPath}");
                return AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            }

            var idleClip = Pick(clips, "idle", "breathing", "standing");
            var walkClip = Pick(clips, "walk", "walking");
            var runClip  = Pick(clips, "run", "running", "jog");

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (controller == null)
            {
                Debug.LogError("[TLC/Character] فشل إنشاء Animator Controller.");
                return null;
            }

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);

            var sm = controller.layers[0].stateMachine;

            var idle = sm.AddState("Idle", new Vector3(300, 60, 0));
            idle.motion = idleClip;

            var walk = sm.AddState("Walk", new Vector3(300, 180, 0));
            walk.motion = walkClip;

            var run = sm.AddState("Run", new Vector3(300, 300, 0));
            run.motion = runClip;

            sm.defaultState = idle;

            // Idle → Walk
            var t1 = idle.AddTransition(walk);
            t1.AddCondition(AnimatorConditionMode.Greater, 0.08f, "Speed");
            t1.hasExitTime = false;
            t1.duration = 0.15f;

            // Walk → Idle
            var t2 = walk.AddTransition(idle);
            t2.AddCondition(AnimatorConditionMode.Less, 0.08f, "Speed");
            t2.hasExitTime = false;
            t2.duration = 0.2f;

            // Walk → Run
            var t3 = walk.AddTransition(run);
            t3.AddCondition(AnimatorConditionMode.Greater, 0.55f, "Speed");
            t3.hasExitTime = false;
            t3.duration = 0.15f;

            // Run → Walk
            var t4 = run.AddTransition(walk);
            t4.AddCondition(AnimatorConditionMode.Less, 0.55f, "Speed");
            t4.hasExitTime = false;
            t4.duration = 0.2f;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TLC/Character] Animator: Idle='{idleClip?.name}' " +
                      $"Walk='{walkClip?.name}' Run='{runClip?.name}'");

            return controller;
        }

        // ────────────────────────── تحقق ──────────────────────────

        [MenuItem("TLC/5b · مسح إحصاءات الشخصيات", priority = 51)]
        public static void ReportCharacters()
        {
            string folder = "Assets/_Project/Art/Characters";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.Log("[TLC/Character] فولدر الشخصيات فاضي — البديل المؤقت هو المستخدم.");
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
            Debug.Log($"[TLC/Character] عدد الموديلات: {guids.Length}");

            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var importer = AssetImporter.GetAtPath(p) as ModelImporter;
                bool humanoid = importer != null && importer.animationType == ModelImporterAnimationType.Human;
                Debug.Log($"  {(humanoid ? "✓" : "✗")} {p}");
            }
        }
    }
}
#endif
