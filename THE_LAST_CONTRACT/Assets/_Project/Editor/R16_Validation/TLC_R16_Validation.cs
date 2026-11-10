// ════════════════════════════════════════════════════════════════════════
//  TLC — R1.6 Validation Suite  ·  Editor-only
//  قائمة: TLC ▸ R1.6
//
//  قواعد التصميم:
//   • Read-only بالكامل: مفيش AssetDatabase.CreateAsset/SaveAssets/DeleteAsset
//     ولا تعديل على GameState أو ملفات الحفظ أو بيانات اللعبة.
//   • ما بتشتغلش تلقائيًا: مفيش [InitializeOnLoad] ولا [DidReloadScripts]
//     ولا AssetPostprocessor — تشتغل بس لما تدوس من المنيو.
//   • أي كتابة = تقرير JSON واحد في جذر المشروع، وبس لما تطلبها صراحة.
// ════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace TLC.EditorTools.R16
{
    public static class R16Validation
    {
        // ── إعدادات المسارات (مطابقة لـ CharacterLibrary) ──
        const string CharacterRoot = "Assets/_Project/Resources/Characters";
        const string ControllerPath = "Assets/_Project/Resources/Animators/TLC_Character.controller";
        const string ReportFileName = "R1_6_VALIDATION.json";

        static readonly (string cast, string key, string file)[] Roster =
        {
            ("Adam",   "adam",   "man-base-character-rigged"),
            ("Murad",  "murad",  "elderly-man-rigged"),
            ("Laila",  "laila",  "woman-rigged"),
            ("Yassin", "yassin", "man-rigged"),
        };

        // الفقرات الإجبارية في أي Humanoid صالح
        static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Head,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
        };

        // ═════════════════════ المنيو ═════════════════════
        [MenuItem("TLC/R1.6/فحص الشخصيات (Humanoid + Resources)", false, 10)]
        public static void MenuCharacters() => RunAndReport(new[] { "characters" }, false);

        [MenuItem("TLC/R1.6/فحص Animator Controller", false, 11)]
        public static void MenuAnimator() => RunAndReport(new[] { "animator" }, false);

        [MenuItem("TLC/R1.6/فحص المواد والشيدر", false, 12)]
        public static void MenuMaterials() => RunAndReport(new[] { "materials" }, false);

        [MenuItem("TLC/R1.6/فحص سلامة المشروع (metas/auto-run)", false, 13)]
        public static void MenuIntegrity() => RunAndReport(new[] { "integrity" }, false);

        [MenuItem("TLC/R1.6/كل الفحوصات + تصدير JSON", false, 30)]
        public static void MenuAll() => RunAndReport(new[] { "characters", "animator", "materials", "integrity" }, true);

        // ═════════════════════ المحرّك ═════════════════════
        public static void RunAndReport(string[] suites, bool exportJson)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("TLC R1.6", "الفحص بيشتغل في وضع التحرير فقط — اخرج من Play Mode الأول.", "تمام");
                return;
            }

            var sb = new StringBuilder();
            var json = new StringBuilder();
            int fails = 0, warns = 0;

            sb.AppendLine($"════ TLC R1.6 Validation · {DateTime.Now:yyyy-MM-dd HH:mm:ss} · Unity {Application.unityVersion} ════");
            json.Append("{\n  \"unityVersion\": \"").Append(Application.unityVersion)
                .Append("\",\n  \"generatedAt\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n");

            if (suites.Contains("characters"))
            {
                sb.AppendLine("\n── ١) الشخصيات: Resources.Load + Humanoid ──");
                json.Append("  \"characters\": [");
                bool first = true;
                foreach (var (cast, key, file) in Roster)
                {
                    string resPath = $"Characters/{key}/{file}";
                    string assetPath = $"{CharacterRoot}/{key}/{file}.fbx";
                    var prefab = Resources.Load<GameObject>(resPath);          // نفس نداء اللعبة
                    bool exists = File.Exists(assetPath);
                    if (prefab == null) fails++;

                    var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                    var subAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                    var avatar = subAssets.OfType<Avatar>().FirstOrDefault();
                    var clips = subAssets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();

                    bool human = importer != null && importer.animationType == ModelImporterAnimationType.Human;
                    bool aValid = avatar != null && avatar.isValid;
                    bool aHuman = avatar != null && avatar.isHuman;
                    int mapped = avatar != null ? avatar.humanDescription.human.Length : 0;

                    // فحص العظام الإجبارية من داخل الـAvatar نفسه (بدون Instantiate)
                    int missingBones = 0;
                    if (aHuman)
                    {
                        var humanBones = avatar.humanDescription.human;
                        foreach (var req in RequiredBones)
                        {
                            bool found = humanBones.Any(h => h.humanName == req.ToString());
                            if (!found) missingBones++;
                        }
                    }
                    if (!human || !aValid || !aHuman || missingBones > 0 || prefab == null) fails++;

                    sb.AppendLine($"  {cast,-7} Resources.Load={(prefab != null ? "✔" : "✘")} · ملف={(exists ? "✔" : "✘")} · " +
                                  $"Humanoid={(human ? "✔" : "✘")} · isValid={(aValid ? "✔" : "✘")} · isHuman={(aHuman ? "✔" : "✘")} · " +
                                  $"عظام={mapped} · ناقص={missingBones} · كليبس={clips.Length} [{string.Join(", ", clips.Select(c => c.name))}]");
                    foreach (var c in clips)
                        if (!c.isHumanMotion) warns++;

                    if (!first) json.Append(",");
                    first = false;
                    json.Append($"\n    {{\"cast\":\"{cast}\",\"resourcesLoad\":{(prefab != null ? "true" : "false")}," +
                                $"\"fileExists\":{(exists ? "true" : "false")},\"humanoid\":{human.ToString().ToLower()}," +
                                $"\"avatarValid\":{aValid.ToString().ToLower()},\"avatarHuman\":{aHuman.ToString().ToLower()}," +
                                $"\"mappedBones\":{mapped},\"missingRequiredBones\":{missingBones}," +
                                $"\"clips\":[{string.Join(",", clips.Select(c => $"{{\"name\":\"{c.name}\",\"length\":{c.length:0.###},\"human\":{c.isHumanMotion.ToString().ToLower()}}}"))}]}}");
                }
                json.Append("],\n");
            }

            if (suites.Contains("animator"))
            {
                sb.AppendLine("\n── ٢) Animator Controller ──");
                var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
                var runtimeCtrl = Resources.Load<RuntimeAnimatorController>("Animators/TLC_Character");
                if (ctrl == null) fails++;
                var states = new List<AnimatorState>();
                if (ctrl != null) foreach (var layer in ctrl.layers) CollectStates(layer.stateMachine, states);
                int missingMotions = states.Count(s => s.motion == null);
                fails += missingMotions;

                sb.AppendLine($"  controller={(ctrl != null ? "✔" : "✘")} · Resources={(runtimeCtrl != null ? "✔" : "✘")} · " +
                              $"states={states.Count} · مفقود={missingMotions}");
                foreach (var s in states)
                    sb.AppendLine($"     • {s.name,-8} → {(s.motion != null ? s.motion.name : "<MISSING>")} " +
                                  $"({(s.motion != null ? s.motion.GetType().Name : "-")})");

                if (ctrl != null)
                {
                    var expected = new[] { "Speed", "IsRunning", "Grounded", "Talking" };
                    var have = ctrl.parameters.Select(p => p.name).ToHashSet();
                    var missingParams = expected.Where(p => !have.Contains(p)).ToArray();
                    if (missingParams.Length > 0) warns++;
                    sb.AppendLine($"  parameters: {string.Join(", ", ctrl.parameters.Select(p => p.name + ":" + p.type))}" +
                                  (missingParams.Length > 0 ? $"  ⚠ ناقص: {string.Join(",", missingParams)}" : "  ✔"));
                    json.Append("  \"animator\": {\"found\":true,\"states\":").Append(states.Count)
                       .Append(",\"missingMotions\":").Append(missingMotions)
                       .Append(",\"params\":[").Append(string.Join(",", ctrl.parameters.Select(p => "\"" + p.name + "\""))).Append("]},\n");
                }
                else json.Append("  \"animator\": {\"found\":false},\n");
            }

            if (suites.Contains("materials"))
            {
                sb.AppendLine("\n── ٣) المواد والشيدر ──");
                json.Append("  \"materials\": [");
                bool first = true;
                foreach (var (cast, key, file) in Roster)
                {
                    var mats = AssetDatabase.LoadAllAssetsAtPath($"{CharacterRoot}/{key}/{file}.fbx").OfType<Material>().ToArray();
                    foreach (var m in mats)
                    {
                        string shader = m.shader != null ? m.shader.name : "<null>";
                        bool urp = shader.StartsWith("Universal Render Pipeline");
                        bool baseMap = m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap") != null;
                        bool normal = m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null;
                        bool gloss = m.HasProperty("_MetallicGlossMap") && m.GetTexture("_MetallicGlossMap") != null;
                        if (!urp || !baseMap) fails++;
                        if (!gloss) warns++;   // معروف من R1.5 — شغل R2
                        sb.AppendLine($"  {cast,-7} shader={shader} · BaseMap={(baseMap ? "✔" : "✘")} · Normal={(normal ? "✔" : "✘")} · " +
                                      $"MetallicGloss={(gloss ? "✔" : "✘ (خرايط metallic/roughness مش مربوطة)")}");
                        if (!first) json.Append(",");
                        first = false;
                        json.Append($"\n    {{\"cast\":\"{cast}\",\"shader\":\"{shader}\",\"baseMap\":{baseMap.ToString().ToLower()}," +
                                    $"\"normal\":{normal.ToString().ToLower()},\"metallicGloss\":{gloss.ToString().ToLower()}}}");
                    }
                }
                json.Append("],\n");
            }

            if (suites.Contains("integrity"))
            {
                sb.AppendLine("\n── ٤) سلامة المشروع ──");
                // أ) أصول من غير meta
                var noMeta = new List<string>();
                foreach (var path in AssetDatabase.GetAllAssetPaths())
                {
                    if (!path.StartsWith("Assets/")) continue;
                    if (AssetDatabase.IsValidFolder(path)) continue;
                    if (path.EndsWith(".meta")) continue;
                    if (!File.Exists(path + ".meta")) noMeta.Add(path);
                }
                // ب) metas يتيمة
                var orphan = new List<string>();
                foreach (var meta in Directory.GetFiles("Assets", "*.meta", SearchOption.AllDirectories))
                {
                    string target = meta.Substring(0, meta.Length - 5).Replace('\\', '/');
                    if (!File.Exists(target) && !AssetDatabase.IsValidFolder(target)) orphan.Add(meta.Replace('\\', '/'));
                }
                // ج) سكربتات Editor بتشتغل تلقائيًا
                var autoRun = new List<string>();
                foreach (var cs in Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories))
                {
                    string txt;
                    try { txt = File.ReadAllText(cs); } catch { continue; }
                    if (System.Text.RegularExpressions.Regex.IsMatch(txt, @"^[ \t]*\[InitializeOnLoad", System.Text.RegularExpressions.RegexOptions.Multiline)) autoRun.Add(cs.Replace('\\', '/'));
                }
                if (orphan.Count > 0) warns++;
                if (autoRun.Count > 0) warns++;

                sb.AppendLine($"  أصول من غير .meta: {noMeta.Count}");
                foreach (var p in noMeta.Take(12)) sb.AppendLine($"     • {p}");
                if (noMeta.Count > 12) sb.AppendLine($"     … و{noMeta.Count - 12} غيرهم");
                sb.AppendLine($"  metas يتيمة (أصلها مش موجود): {orphan.Count}");
                foreach (var p in orphan) sb.AppendLine($"     • {p}");
                sb.AppendLine($"  سكربتات [InitializeOnLoad] (بتشتغل لوحدها عند فتح المشروع): {autoRun.Count}");
                foreach (var p in autoRun) sb.AppendLine($"     • {p}");

                json.Append("  \"integrity\": {\"assetsWithoutMeta\":").Append(noMeta.Count)
                   .Append(",\"orphanMetas\":[").Append(string.Join(",", orphan.Select(o => "\"" + o + "\"")))
                   .Append("],\"autoRunScripts\":").Append(autoRun.Count).Append("},\n");
            }

            sb.AppendLine($"\n════ النتيجة: fails={fails} · warnings={warns} ════");
            json.Append("  \"fails\": ").Append(fails).Append(",\n  \"warnings\": ").Append(warns).Append("\n}\n");

            Debug.Log(sb.ToString());
            if (exportJson)
            {
                string root = Directory.GetParent(Application.dataPath).FullName;
                string outPath = Path.Combine(root, ReportFileName);
                File.WriteAllText(outPath, json.ToString(), new UTF8Encoding(false));
                Debug.Log($"[TLC/R1.6] التقرير اتصدّر: {outPath}");
            }
            else if (fails > 0)
                EditorUtility.DisplayDialog("TLC R1.6", $"الفحص خلص: fails={fails} · warnings={warns}\nالتفاصيل في Console.", "تمام");
            else
                EditorUtility.DisplayDialog("TLC R1.6", $"كل الفحوصات نجحت ✔ (warnings={warns})\nالتفاصيل في Console.", "تمام");
        }

        static void CollectStates(AnimatorStateMachine sm, List<AnimatorState> outStates)
        {
            if (sm == null) return;
            outStates.AddRange(sm.states.Select(s => s.state).Where(s => s != null));
            foreach (var child in sm.stateMachines) CollectStates(child.stateMachine, outStates);
        }

        // نقطة دخول للـbatchmode (اختيارية): Unity -executeMethod TLC.EditorTools.R16.R16Validation.RunAllBatch
        public static void RunAllBatch()
        {
            RunAndReport(new[] { "characters", "animator", "materials", "integrity" }, true);
        }
    }
}
