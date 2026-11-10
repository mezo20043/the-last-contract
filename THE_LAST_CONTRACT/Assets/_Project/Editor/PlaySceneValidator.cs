#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLC.Bootstrap;
using TLC.CameraRig;
using TLC.Player;
using TLC.UI;
using TLC.World;

namespace TLC.EditorTools
{
    /// <summary>
    /// فحص مشهد اللعب جوه المحرر: Missing Scripts، مراجع ناقصة، وكائنات مطلوبة.
    ///
    /// ده الفحص اللي بيقفل البند «Missing References = 0» داخل Unity نفسها —
    /// لكنه ما بيشتغلش غير لما المشروع يتفتح في المحرر. لحد ساعتها
    /// الحكم الموجود في التقارير هو تحليل ساكن مش نتيجة تشغيل.
    /// </summary>
    public static class PlaySceneValidator
    {
        public static bool Validate(string scenePath = PlaySceneBuilder.PlayPath)
        {
            var report = new StringBuilder();
            report.AppendLine("──── فحص مشهد اللعب ────");
            report.AppendLine($"الملف: {scenePath}");

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[TLC/Validate] المشهد مش موجود: {scenePath}");
                return false;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            bool ok = true;

            // ── ١) سكربتات ناقصة ──
            int missing = 0;
            var missingNames = new List<string>();

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var comp in root.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null)
                    {
                        missing++;
                        missingNames.Add(root.name);
                    }
                }
            }

            if (missing == 0) report.AppendLine("✓ Missing Scripts: 0");
            else
            {
                ok = false;
                report.AppendLine($"✗ Missing Scripts: {missing}");
                foreach (var n in missingNames) report.AppendLine($"    · {n}");
            }

            // ── ٢) كائنات مطلوبة ──
            ok &= Require<Camera>(report, "Main Camera / Camera");
            ok &= Require<ThirdPersonCameraRig>(report, "Main Camera / ThirdPersonCameraRig");
            ok &= Require<AdamBuilder>(report, "ADAM_MANSOUR / AdamBuilder");
            ok &= Require<ApartmentBuilder>(report, "WORLD/Apartment / ApartmentBuilder");
            ok &= Require<LightingRig>(report, "WORLD/Lighting / LightingRig");
            ok &= Require<MobileControls>(report, "MOBILE_CONTROLS / MobileControls");
            ok &= Require<OrientationWatcher>(report, "OrientationWatcher");
            ok &= Require<GameBootstrap>(report, "GAME / GameBootstrap");

            var sun = Object.FindObjectOfType<Light>();
            if (sun != null && sun.type == LightType.Directional)
                report.AppendLine("✓ ضوء اتجاهي موجود");
            else
            {
                ok = false;
                report.AppendLine("✗ مفيش ضوء اتجاهي (Directional Light)");
            }

            // ── ٣) الكاميرا موجّهة على آدم ──
            var rig = Object.FindObjectOfType<ThirdPersonCameraRig>();
            var adam = Object.FindObjectOfType<AdamBuilder>();
            var rigSo = rig != null ? new SerializedObject(rig) : null;
            var targetProp = rigSo?.FindProperty("target");

            if (rig != null && adam != null && targetProp != null &&
                targetProp.objectReferenceValue == adam.transform)
            {
                report.AppendLine("✓ ThirdPersonCameraRig.target → آدم (مرجع مش null)");
            }
            else
            {
                ok = false;
                report.AppendLine("✗ ربط الكاميرا بآدم مش مظبوط");
            }

            // ── ٤) المشهدين في Build Settings ──
            bool mainIn = false, playIn = false;
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (!s.enabled) continue;
                if (s.path.EndsWith("Main.unity")) mainIn = true;
                if (s.path.EndsWith("Play.unity")) playIn = true;
            }

            if (mainIn && playIn) report.AppendLine("✓ Main و Play في Build Settings");
            else
            {
                ok = false;
                report.AppendLine($"✗ Build Settings ناقص — Main:{mainIn} Play:{playIn}");
            }

            // ── ٥) Animator Controller ──
            var controller = Resources.Load<RuntimeAnimatorController>("Animators/TLC_Character");
            if (controller != null)
                report.AppendLine("✓ Animators/TLC_Character موجود (هيتربط وقت التشغيل)");
            else
                report.AppendLine("! Animators/TLC_Character مش موجود — " +
                                  "شغّل TLC/بند Animator أو افتح المشروع تاني لتوليده");

            report.AppendLine(ok
                ? "── النتيجة: المشهد سليم (فحص محرر) ──"
                : "── النتيجة: فيه بنود ناقصة فوق ──");

            if (ok) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());

            return ok;
        }

        static bool Require<T>(StringBuilder report, string label) where T : Component
        {
            if (Object.FindObjectOfType<T>() != null)
            {
                report.AppendLine($"✓ {label}");
                return true;
            }

            report.AppendLine($"✗ ناقص: {label}");
            return false;
        }

        [MenuItem("TLC/11 · فحص مشهد اللعب (Missing Scripts)", priority = 11)]
        public static void ValidateMenu() => Validate();
    }
}
#endif
