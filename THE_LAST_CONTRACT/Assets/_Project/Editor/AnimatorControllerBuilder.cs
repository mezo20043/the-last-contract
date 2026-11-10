using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TLC.EditorTools
{
    /// <summary>
    /// يبني Animator Controller حقيقي للشخصيات من كليبات الـ FBX.
    ///
    /// المصدر: ملفات الشخصيات فيها أخذتان مسمّاتان <c>walk</c> و <c>run</c>
    /// (متحقّق منها بفك الـ FBX — انظر IMPORT_REPORT.md).
    ///
    /// Idle مش موجود في الملفات، فبيتولّد كليب تنفّس حقيقي بمنحنيات على
    /// العظام — المسارات بتُحسب من هيكل الموديل نفسه بعد الاستيراد،
    /// مش مكتوبة بالتخمين.
    ///
    /// المخرج: <c>Resources/Animators/TLC_Character.controller</c> عشان
    /// يتلقط وقت التشغيل بدون أي ربط يدوي.
    /// </summary>
    public static class AnimatorControllerBuilder
    {
        public const string ControllerPath = "Assets/_Project/Resources/Animators/TLC_Character.controller";
        const string ModelDir = "Assets/_Project/Resources/Characters";

        [MenuItem("TLC/8 · إنشاء Animator Controller", false, 80)]
        public static void Build()
        {
            var dir = Path.GetDirectoryName(ControllerPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(ControllerPath)) AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("[TLC/Anim] فشل إنشاء الـ controller.");
                return;
            }

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);

            var machine = controller.layers[0].stateMachine;

            AnimationClip walk = FindClip("adam", "walk");
            AnimationClip run = FindClip("adam", "run");
            AnimationClip idle = FindClip("adam", "idle") ?? BuildIdleClip("adam");

            var idleState = machine.AddState("Idle");
            var walkState = machine.AddState("Walk");
            var runState = machine.AddState("Run");

            idleState.motion = idle;
            walkState.motion = walk;
            runState.motion = run;

            machine.defaultState = idleState;

            if (idle != null) idleState.speed = 1f;
            if (walk != null) walkState.speed = 1.25f;     // مشية موبايل أسرع شوية
            if (run != null) runState.speed = 1.35f;

            AddTransition(idleState, walkState, AnimatorConditionMode.Greater, 0.15f, "Speed");
            AddTransition(walkState, runState, AnimatorConditionMode.Greater, 4.0f, "Speed");
            AddTransition(runState, walkState, AnimatorConditionMode.Less, 4.0f, "Speed");
            AddTransition(walkState, idleState, AnimatorConditionMode.Less, 0.15f, "Speed");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TLC/Anim] اتعمل: {ControllerPath} · idle={(idle != null)} walk={(walk != null)} run={(run != null)}");
        }

        static void AddTransition(AnimatorState from, AnimatorState to,
            AnimatorConditionMode mode, float threshold, string param)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0.18f;
            t.hasFixedDuration = true;
            t.AddCondition(mode, threshold, param);
        }

        /// <summary>يلاقي كليب بالاسم داخل موديل مستورد.</summary>
        static AnimationClip FindClip(string characterKey, string clipName)
        {
            string path = FindModelPath(characterKey);
            if (path == null) return null;

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip &&
                    !clip.name.StartsWith("__preview__") &&
                    clip.name.ToLowerInvariant().Contains(clipName.ToLowerInvariant()))
                    return clip;
            }
            return null;
        }

        static string FindModelPath(string key)
        {
            string dir = Path.Combine(ModelDir, key);
            if (!Directory.Exists(dir)) return null;
            foreach (var f in Directory.GetFiles(dir, "*.fbx")) return f.Replace('\\', '/');
            return null;
        }

        /// <summary>
        /// كليب Idle: تنفّس هادي على العمود الفقري والراس.
        /// المسارات بتتقري من الموديل الفعلي، فإن اختلفت التسمية مايحصلش خطأ.
        /// </summary>
        static AnimationClip BuildIdleClip(string characterKey)
        {
            string modelPath = FindModelPath(characterKey);
            if (modelPath == null) return null;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) return null;

            var bones = new Dictionary<string, string>();
            foreach (var t in prefab.GetComponentsInChildren<Transform>())
            {
                string lower = t.name.ToLowerInvariant();
                if (lower.Contains("spine") && !bones.ContainsKey("spine")) bones["spine"] = PathOf(t, prefab.transform);
                else if (lower == "neck" || lower.Contains("neck")) bones["neck"] = PathOf(t, prefab.transform);
                else if (lower == "head" || lower.Contains("head")) bones["head"] = PathOf(t, prefab.transform);
                else if (lower == "hips" || lower.Contains("hips")) bones["hips"] = PathOf(t, prefab.transform);
            }

            if (bones.Count == 0)
            {
                Debug.LogWarning("[TLC/Anim] مفيش عظام اتلاقت لبناء Idle — الحالة هتكون ساكنة.");
                return null;
            }

            var clip = new AnimationClip { name = "idle", frameRate = 30f };

            // تنفّس: دورة ٤ ثواني — صعود ونزول بسيط على العمود الفقري
            AddRotationCurve(clip, bones, "spine", "localEulerAnglesRaw.x", -1.2f, 0.9f, 4f);
            AddRotationCurve(clip, bones, "neck", "localEulerAnglesRaw.x", 0f, -0.8f, 4f);
            AddRotationCurve(clip, bones, "head", "localEulerAnglesRaw.y", 0f, 2.5f, 7f);

            AssetDatabase.CreateAsset(clip, "Assets/_Project/Resources/Animators/idle.anim");
            AssetDatabase.SaveAssets();
            Debug.Log($"[TLC/Anim] اتولّد كليب Idle — {bones.Count} عظمة.");
            return clip;
        }

        static void AddRotationCurve(AnimationClip clip, Dictionary<string, string> bones,
            string key, string property, float from, float to, float period)
        {
            if (!bones.TryGetValue(key, out var path) || string.IsNullOrEmpty(path)) return;

            var curve = AnimationCurve.EaseInOut(0f, from, period, to);
            var post = AnimationCurve.EaseInOut(period, to, period * 2f, from);
            var combined = new AnimationCurve();
            for (int i = 0; i < curve.length; i++) combined.AddKey(curve[i]);
            for (int i = 1; i < post.length; i++) combined.AddKey(post[i]);

            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(path, typeof(Transform), property), combined);
        }

        static string PathOf(Transform t, Transform root)
        {
            if (t == root) return "";
            string path = t.name;
            var p = t.parent;
            while (p != null && p != root)
            {
                path = p.name + "/" + path;
                p = p.parent;
            }
            return path;
        }

        /// <summary>يتأكد إن الـ controller موجود — يتنادى تلقائياً عند فتح المشروع.</summary>
        public static void EnsureExists()
        {
            if (!File.Exists(ControllerPath)) Build();
        }
    }

    /// <summary>يشغّل التهيئة مرة واحدة عند أول فتح للمشروع.</summary>
    [InitializeOnLoad]
    public static class FirstOpenSetup
    {
        static FirstOpenSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (SessionState.GetBool("TLC_FirstSetupDone", false)) return;

                SessionState.SetBool("TLC_FirstSetupDone", true);
                AnimatorControllerBuilder.EnsureExists();
            };
        }
    }
}
