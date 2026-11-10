// TLC — R1.5 Validator (Editor only · للقراءة فقط على الأصول)
// الاستخدام:
//   ١) منيو: TLC ▸ R1.5 Validation
//   ٢) أو batchmode:
//      Unity.exe -batchmode -quit -projectPath <path> -executeMethod TLC.EditorTools.R15Validator.RunBatch
//
// بيفحص: الـAvatar الـHumanoid · أخطاء الاستيراد · كليبس الـController · الشيدر (URP)
// ويكتب النتيجة في: <ProjectRoot>/R1_5_VALIDATION.json
// ما بيعدّلش أي أصل — قراءة فقط.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TLC.EditorTools
{
    public static class R15Validator
    {
        static readonly string[] Models =
        {
            "Assets/_Project/Resources/Characters/adam/man-base-character-rigged.fbx",
            "Assets/_Project/Resources/Characters/laila/woman-rigged.fbx",
            "Assets/_Project/Resources/Characters/murad/elderly-man-rigged.fbx",
            "Assets/_Project/Resources/Characters/yassin/man-rigged.fbx",
        };

        const string ControllerPath = "Assets/_Project/Resources/Animators/TLC_Character.controller";

        [MenuItem("TLC/R1.5 Validation")]
        public static void RunMenu() { RunBatch(); EditorUtility.DisplayDialog("TLC", "خلص الفحص — التقرير في R1_5_VALIDATION.json", "تمام"); }

        public static void RunBatch()
        {
            var sb = new StringBuilder();
            int fails = 0, warns = 0;
            sb.Append("{\n  \"unityVersion\": \"").Append(Application.unityVersion).Append("\",\n");
            sb.Append("  \"generatedAt\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n  \"models\": [\n");

            for (int i = 0; i < Models.Length; i++)
            {
                var path = Models[i];
                sb.Append("    {\n      \"path\": \"").Append(path).Append("\",\n");
                bool exists = File.Exists(path);
                sb.Append("      \"existsOnDisk\": ").Append(exists ? "true" : "false").Append(",\n");
                if (!exists) { fails++; sb.Append("      \"error\": \"الأصل مش موجود\"\n    }"); if (i < Models.Length - 1) sb.Append(","); sb.Append("\n"); continue; }

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    fails++;
                    sb.Append("      \"error\": \"مفيش ModelImporter — الأصل ما اتستوردش\"\n    }");
                    if (i < Models.Length - 1) sb.Append(",");
                    sb.Append("\n");
                    continue;
                }

                bool human = importer.animationType == ModelImporterAnimationType.Human;
                if (!human) fails++;
                sb.Append("      \"animationType\": \"").Append(importer.animationType).Append("\",\n");
                sb.Append("      \"avatarSetup\": \"").Append(importer.avatarSetup).Append("\",\n");

                // الـAvatar
                var all = AssetDatabase.LoadAllAssetsAtPath(path);
                var avatar = all.OfType<Avatar>().FirstOrDefault();
                bool avatarValid = avatar != null && avatar.isValid;
                bool avatarHuman = avatar != null && avatar.isHuman;
                if (!avatarValid || !avatarHuman) fails++;
                sb.Append("      \"avatarFound\": ").Append(avatar != null ? "true" : "false").Append(",\n");
                sb.Append("      \"avatarIsValid\": ").Append(avatarValid ? "true" : "false").Append(",\n");
                sb.Append("      \"avatarIsHuman\": ").Append(avatarHuman ? "true" : "false").Append(",\n");
                if (avatar != null)
                {
                    var hb = avatar.humanDescription.human;
                    var sk = avatar.humanDescription.skeleton;
                    sb.Append("      \"mappedHumanBones\": ").Append(hb != null ? hb.Length : 0).Append(",\n");
                    sb.Append("      \"skeletonBones\": ").Append(sk != null ? sk.Length : 0).Append(",\n");
                }

                // الكليبس
                var clips = all.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
                sb.Append("      \"clips\": [");
                sb.Append(string.Join(", ", clips.Select(c =>
                    "{\"name\":\"" + Esc(c.name) + "\",\"length\":" + c.length.ToString("0.###") + ",\"humanMotion\":" + (c.isHumanMotion ? "true" : "false") + "}")));
                sb.Append("],\n");

                // المواد والشيدر
                var mats = all.OfType<Material>().ToArray();
                var shaders = mats.Select(m => m.shader != null ? m.shader.name : "null").Distinct().ToArray();
                bool builtInStandard = shaders.Any(s => s == "Standard" || s == "Standard (Specular setup)");
                if (builtInStandard) warns++;
                sb.Append("      \"materialCount\": ").Append(mats.Length).Append(",\n");
                sb.Append("      \"shaders\": [").Append(string.Join(", ", shaders.Select(s => "\"" + Esc(s) + "\""))).Append("]");

                Debug.Log($"[TLC/R1.5] {Path.GetFileNameWithoutExtension(path)} → avatar valid={avatarValid} human={avatarHuman} · clips={clips.Length} · shaders={string.Join("|", shaders)}");
                sb.Append("\n    }");
                if (i < Models.Length - 1) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("  ],\n");

            // الـAnimator Controller
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            sb.Append("  \"controller\": {\n    \"found\": ").Append(ctrl != null ? "true" : "false").Append(",\n");
            if (ctrl == null) { fails++; sb.Append("    \"error\": \"الـcontroller مش موجود\"\n  },\n"); }
            else
            {
                var states = new List<AnimatorState>();
                foreach (var layer in ctrl.layers) Collect(layer.stateMachine, states);
                var lines = states.Select(s =>
                {
                    bool missing = s.motion == null;
                    if (missing) { }
                    return "{\"state\":\"" + Esc(s.name) + "\",\"motion\":\"" + (s.motion != null ? Esc(s.motion.name) : "<MISSING>") + "\",\"type\":\"" + (s.motion != null ? s.motion.GetType().Name : "null") + "\"}";
                }).ToArray();
                int missingCount = states.Count(s => s.motion == null);
                fails += missingCount;
                sb.Append("    \"stateCount\": ").Append(states.Count).Append(",\n");
                sb.Append("    \"missingMotions\": ").Append(missingCount).Append(",\n");
                sb.Append("    \"parameters\": [").Append(string.Join(", ", ctrl.parameters.Select(p => "\"" + Esc(p.name) + ":" + p.type + "\""))).Append("],\n");
                sb.Append("    \"states\": [").Append(string.Join(", ", lines)).Append("]\n  },\n");
                Debug.Log($"[TLC/R1.5] Controller: {states.Count} states · missing motions = {missingCount}");
            }

            sb.Append("  \"fails\": ").Append(fails).Append(",\n  \"warnings\": ").Append(warns).Append("\n}\n");

            var outPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "R1_5_VALIDATION.json");
            File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[TLC/R1.5] التقرير اتكتب: {outPath} · fails={fails} · warnings={warns}");
        }

        static void Collect(AnimatorStateMachine sm, List<AnimatorState> outStates)
        {
            if (sm == null) return;
            outStates.AddRange(sm.states.Select(s => s.state).Where(s => s != null));
            foreach (var child in sm.stateMachines) Collect(child.stateMachine, outStates);
        }

        static string Esc(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
