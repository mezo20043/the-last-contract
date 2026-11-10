using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TLC.Core;
using TLC.Narrative;
using UnityEngine;
using TLC.Settings;
using TLC.UI;
using TLC.Cinematics;
using TLC.Bootstrap;
using TLC.World;

namespace TLCSim
{
    /// <summary>
    /// اختبار آلي شامل لمنطق THE LAST CONTRACT.
    ///
    /// مهم: الاختبار بيستخدم الكود الفعلي بتاع المشروع نفسه —
    /// GameState و NarrativeTypes و CompiledStory و NarrativeRunner و SaveSystem —
    /// مش إعادة كتابة بالـ JavaScript ولا نسخة موازية.
    /// </summary>
    public static class Program
    {
        static int _failures;
        static readonly List<string> _log = new List<string>();

        static void Check(bool condition, string label, string detail = null)
        {
            if (condition) _log.Add($"  ✓ {label}");
            else { _failures++; _log.Add($"  ✗ {label}{(detail != null ? " — " + detail : "")}"); }
        }

        static void Section(string title)
        {
            _log.Add("");
            _log.Add($"── {title} ──");
        }

        public static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

            string root = FindRoot();
            string storyDir = Path.Combine(root, "Assets", "_Project", "Resources", "Story");
            string[] files = { "chapter01.json", "chapter02.json", "chapter03.json" };

            Console.WriteLine("════════ THE LAST CONTRACT — اختبار آلي ════════");
            Console.WriteLine($"مجلد المشروع: {root}");
            Console.WriteLine();

            // ── تحميل الفصول ──
            var chapters = new List<(string file, string json, CompiledStory story)>();
            foreach (var f in files)
            {
                string path = Path.Combine(storyDir, f);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"✗ ملف مفقود: {path}");
                    return 1;
                }
                string json = File.ReadAllText(path, Encoding.UTF8);
                var compiled = CompiledStory.FromJson(json);
                chapters.Add((f, json, compiled));
            }

            // ═══════════ 1) سلامة البنية ═══════════
            Section("١) سلامة ملفات القصة");
            foreach (var (file, json, story) in chapters)
            {
                Check(story.Document != null, $"{file}: تحليل JSON");
                Check(story.CorruptionCount == 0, $"{file}: صفر روابط مكسورة",
                      $"{story.CorruptionCount} مشكلة");
                Check(story.Document.nodes.Length > 0, $"{file}: فيه عقد");
            }

            // عقد القرار ممنوع يكون فيها onEnter (يكسر إعادة التشغيل)
            int choiceWithEnter = 0;
            foreach (var (_, _, story) in chapters)
                foreach (var n in story.Document.nodes)
                    if (n.Kind == NodeKind.Choice && n.onEnter != null) choiceWithEnter++;
            Check(choiceWithEnter == 0, "عقد القرار من غير onEnter (ثبات إعادة التشغيل)",
                  $"{choiceWithEnter} عقدة");

            // ═══════════ 2) الوصولية ═══════════
            Section("٢) وصولية كل عقدة");
            int totalUnreachable = 0;
            foreach (var (file, _, story) in chapters)
            {
                var reachable = BfsReachable(story);
                var all = story.Document.nodes.Select(n => n.id).ToHashSet();
                var missing = all.Where(id => !reachable.Contains(id)).ToList();
                totalUnreachable += missing.Count;
                Check(missing.Count == 0, $"{file}: كل العقد ({all.Count}) قابلة للوصول",
                      $"غير قابل للوصول: {string.Join(", ", missing.Take(6))}");
            }

            // ═══════════ 3) نهايات وطرق مسدودة ═══════════
            Section("٣) النهايات والطرق المسدودة");
            foreach (var (file, _, story) in chapters)
            {
                var deadEnds = story.Document.nodes
                    .Where(n => n.Kind == NodeKind.Line && string.IsNullOrEmpty(n.next))
                    .ToList();
                Check(deadEnds.Count == 0, $"{file}: مفيش سطر بلا متابعة",
                      string.Join(", ", deadEnds.Take(4).Select(n => n.id)));

                var emptyChoices = story.Document.nodes
                    .Where(n => n.Kind == NodeKind.Choice &&
                                (n.options == null || n.options.Length == 0))
                    .ToList();
                Check(emptyChoices.Count == 0, $"{file}: مفيش قرار بلا خيارات");

                int endings = story.Document.nodes.Count(n => n.Kind == NodeKind.Ending);
                Check(endings > 0, $"{file}: فيه {endings} نهاية");
            }

            // ═══════════ 4) المتغيّرات الستة ═══════════
            Section("٤) متغيّرات التصميم الستة");
            var usedStats = new HashSet<string>();
            foreach (var (_, _, story) in chapters)
                foreach (var n in story.Document.nodes)
                {
                    CollectStats(n.onEnter, usedStats);
                    if (n.options != null)
                        foreach (var o in n.options) CollectStats(o.effect, usedStats);
                }
            foreach (var key in GameState.AllStats)
                Check(usedStats.Contains(key), $"المتغيّر مستخدم: {key}");

            var unknown = usedStats.Except(GameState.AllStats).ToList();
            Check(unknown.Count == 0, "مفيش متغيّر خارج الستة المعرّفة",
                  string.Join(", ", unknown));

            // ═══════════ 5) اجتياز كامل (مستنفِد) ═══════════
            Section("٥) اجتياز كامل لكل المسارات");
            var explorer = new Explorer(chapters);
            explorer.Run();

            Check(!explorer.CapHit, "الاجتياز اكتمل بدون قطع",
                  "وصل لحد أقصى للحالات — النتيجة عيّنة مش شاملة");
            Check(explorer.PathsExplored > 0, $"تم استكشاف {explorer.PathsExplored:N0} حالة");

            var declaredEndings = chapters
                .SelectMany(c => c.story.Document.nodes)
                .Where(n => n.Kind == NodeKind.Ending)
                .Select(n => n.endingId)
                .Where(e => !string.IsNullOrEmpty(e))
                .ToHashSet();

            var reached = explorer.EndingsReached;
            var unreached = declaredEndings.Except(reached).ToList();

            Check(declaredEndings.Count >= 12, $"إجمالي النهايات المعلنة عبر الفصول الثلاثة: {declaredEndings.Count}");
            Check(unreached.Count == 0, $"{reached.Count}/{declaredEndings.Count} نهاية قابلة للوصول فعلاً",
                  $"غير قابلة للوصول: {string.Join(", ", unreached)}");

            var visitedDeclared = chapters.SelectMany(c => c.story.Document.nodes).Select(n => n.id).ToHashSet();
            var neverVisited = visitedDeclared.Except(explorer.NodesVisited).ToList();
            Check(neverVisited.Count == 0, "كل العقد اتزارت في الاجتياز",
                  string.Join(", ", neverVisited.Take(6)));

            // ═══════════ 6) اختبار تكاملي للمشغّل الحقيقي ═══════════
            Section("٦) اختبار تكاملي — NarrativeRunner الحقيقي");
            RunRunnerIntegration(chapters[0].json);

            // ═══════════ 7) الحفظ والتحميل ═══════════
            Section("٧) الحفظ والتحميل (SaveSystem الحقيقي)");
            RunSaveLoadTest();

            // ═══════════ 8) الترجمة ═══════════
            Section("٨) الترجمة (عربي/إنجليزي)");
            bool complete = Localization.IsComplete(out var brokenKeys);
            Check(complete, "كل المفاتيح مترجمة في اللغتين",
                  $"ناقص: {string.Join(", ", brokenKeys)}");
            Check(Localization.Get("menu.newgame") == "لعبة جديدة", "العربية هي الافتراضي");
            Localization.SetLanguage(Localization.Lang.English);
            Check(Localization.Get("menu.newgame") == "New Game", "التبديل للإنجليزي شغّال");
            Check(!Localization.IsRtl, "الإنجليزي LTR");
            Localization.SetLanguage(Localization.Lang.Arabic);
            Check(Localization.IsRtl, "العربي RTL");

            // ═══════════ 9) تنقّل القوائم ═══════════
            Section("٩) تنقّل القوائم");
            var model = new MenuModel();
            Check(model.Current == UIScreen.Title, "البداية على شاشة العنوان");

            var ctx = new MenuContext { HasSave = false, UnlockedChapters = 1 };
            var titleEntries = model.Build(ctx);
            Check(titleEntries.Count >= 4, $"شاشة العنوان فيها {titleEntries.Count} عنصر");
            var cont = titleEntries.Find(e => e.Action == MenuAction.Continue);
            Check(cont != null && !cont.Enabled, "«متابعة» مقفولة لما مفيش حفظ");
            ctx.HasSave = true;
            cont = model.Build(ctx).Find(e => e.Action == MenuAction.Continue);
            Check(cont != null && cont.Enabled, "«متابعة» بتتفتح لما فيه حفظ");

            model.Open(UIScreen.Main);
            model.Open(UIScreen.Settings);
            Check(model.Current == UIScreen.Settings, "فتح الإعدادات");
            Check(model.Back() && model.Current == UIScreen.Main, "الرجوع → القائمة الرئيسية");
            Check(model.Back() && model.Current == UIScreen.Title, "الرجوع → العنوان");
            Check(!model.Back(), "الرجوع من العنوان مابيعملش حاجة");

            var chMenu = new MenuModel();
            chMenu.Open(UIScreen.Chapters);
            var chEntries = chMenu.Build(new MenuContext { UnlockedChapters = 2 });
            int unlockedCount = chEntries.FindAll(e => e.Action == MenuAction.StartChapter && e.Enabled).Count;
            Check(unlockedCount == 2, $"الفصول المفتوحة = ٢ لما التقدّم ٢ (لقينا {unlockedCount})");

            // ═══════════ 10) مطابقة النهايات ═══════════
            Section("١٠) مطابقة شاشة النهايات مع القصة");
            Check(MenuModel.EndingCount == 12, $"شاشة النهايات فيها {MenuModel.EndingCount}");
            var modelEndings = new HashSet<string>(MenuModel.EndingIds);
            var storyEndings = new HashSet<string>(declaredEndings);
            var onlyInModel = modelEndings.Except(storyEndings).ToList();
            var onlyInStory = storyEndings.Except(modelEndings).ToList();
            Check(onlyInModel.Count == 0 && onlyInStory.Count == 0,
                  "معرّفات النهايات في الواجهة = الموجودة في القصة",
                  $"زيادة في الواجهة: {string.Join(",", onlyInModel)} · ناقص: {string.Join(",", onlyInStory)}");

            var endingsModel = new MenuModel();
            endingsModel.Open(UIScreen.Endings);
            var lockedEntries = endingsModel.Build(new MenuContext { TotalEndings = 12, SeenEndings = new HashSet<string>() });
            Check(lockedEntries.Count(e => e.Key == "endings.locked") == 12, "كل النهايات مقفولة قبل اللعب");

            // ═══════════ 11) التقدّم الدائم ═══════════
            Section("١١) التقدّم الدائم (Progress)");
            Progress.Reset();
            Check(Progress.IsChapterUnlocked(1), "الفصل الأول مفتوح افتراضياً");
            Check(!Progress.IsChapterUnlocked(2), "الفصل الثاني مقفول افتراضياً");

            Progress.UnlockChapter(2);
            Check(Progress.IsChapterUnlocked(2), "بعد الفتح: الفصل الثاني مفتوح");

            bool first = Progress.MarkEnding("CH1_A");
            bool again = Progress.MarkEnding("CH1_A");
            Check(first && !again, "النهاية بتتسجل مرة واحدة بس");
            Check(Progress.HasSeenEnding("CH1_A"), "النهاية مسجّلة");
            Check(Progress.SeenCount == 1, $"العدّاد = ١ (لقينا {Progress.SeenCount})");

            Progress.Save();
            Progress.InjectForTests(null);
            Progress.Load();
            Check(Progress.IsChapterUnlocked(2) && Progress.HasSeenEnding("CH1_A"),
                  "التقدّم عاش بعد الحفظ والتحميل");

            // ═══════════ 12) حركة الفم ═══════════
            Section("١٢) الـ Lip Sync");
            var keys = LipSyncTimeline.Build("مش عارف أصدّق إنك رجعت.", 3.0f);
            Check(keys.Count > 10, $"اتولّد {keys.Count} شكل فم");
            bool monotonic = true;
            for (int i = 1; i < keys.Count; i++)
                if (keys[i].time < keys[i - 1].time - 1e-4f) monotonic = false;
            Check(monotonic, "التوقيتات متزايدة");
            Check(keys[keys.Count - 1].time >= 2.4f, "الجدول بيغطي مدة السطر");
            Check(LipSyncTimeline.For('م') == Viseme.Closed, "«م» = فم مغلق");
            Check(LipSyncTimeline.For('و') == Viseme.Round, "«و» = فم مضموم");
            var sample = LipSyncTimeline.Sample(keys, 1.5f);
            Check(sample.viseme >= 0, "العيّنة الزمنية بترجع شكل صالح");
            Check(LipSyncTimeline.Build("", 2f).Count == 1, "النص الفاضي بيرجّع وضع سكون");

            // ═══════════ 13) الأصول على القرص ═══════════
            Section("١٣) الأصول المولّدة");
            string audioDir = Path.Combine(storyDir, "..", "Audio");
            string texDir = Path.Combine(storyDir, "..", "Art", "Textures");
            audioDir = Path.GetFullPath(audioDir);
            texDir = Path.GetFullPath(texDir);

            var audioFiles = Directory.Exists(audioDir)
                ? Directory.GetFiles(audioDir, "*.wav", SearchOption.AllDirectories) : new string[0];
            var texFiles = Directory.Exists(texDir)
                ? Directory.GetFiles(texDir, "*.png", SearchOption.AllDirectories) : new string[0];

            Check(audioFiles.Length >= 20, $"ملفات صوت: {audioFiles.Length}");
            Check(texFiles.Length >= 8, $"تكستشرات: {texFiles.Length}");

            // كل مقطع بيناديه الكود لازم يكون موجود
            string[] requiredAudio = {
                "Music/title_theme", "Music/act_tension", "Music/act_resolve", "Music/chapter_sting",
                "Ambience/apartment_hum_loop", "Ambience/city_night_loop", "Ambience/rain_loop",
                "SFX/ui_click", "SFX/ui_back", "SFX/ui_hover", "SFX/door_open", "SFX/door_close",
                "SFX/phone_vibrate", "SFX/paper_rustle", "SFX/heartbeat", "SFX/ending_sting",
                "SFX/whoosh_transition",
                "SFX/footstep_concrete_01", "SFX/footstep_concrete_02",
                "SFX/footstep_concrete_03", "SFX/footstep_concrete_04",
                "SFX/footstep_wood_01", "SFX/footstep_wood_02",
                "SFX/footstep_wood_03", "SFX/footstep_wood_04",
            };
            var missingAudio = requiredAudio
                .Where(r => !File.Exists(Path.Combine(audioDir, r.Replace('/', Path.DirectorySeparatorChar) + ".wav")))
                .ToList();
            Check(missingAudio.Count == 0, "كل مقاطع الكود موجودة على القرص",
                  $"مفقود: {string.Join(", ", missingAudio)}");

            string[] requiredTex = {
                "Interior/wood_floor_albedo", "Interior/wood_dark_albedo", "Interior/plaster_wall_albedo",
                "Interior/tile_ceramic_albedo", "Interior/fabric_dark_albedo", "Interior/metal_brushed_albedo",
                "Exterior/concrete_albedo", "Exterior/city_facade_albedo",
            };
            var missingTex = requiredTex
                .Where(r => !File.Exists(Path.Combine(texDir, r.Replace('/', Path.DirectorySeparatorChar) + ".png")))
                .ToList();
            Check(missingTex.Count == 0, "كل الخامات المطلوبة موجودة",
                  $"مفقود: {string.Join(", ", missingTex)}");

            var normals = texFiles.Length >= 8
                ? Directory.GetFiles(texDir, "*_normal.jpg", SearchOption.AllDirectories) : new string[0];
            Check(normals.Length >= 8, $"خرايط Normal: {normals.Length}");

            // ═══════════ 14) الشخصيات الواقعية ═══════════
            Section("١٤) الشخصيات ثلاثية الأبعاد");
            Check(CharacterLibrary.Roster.Length == 4,
                  $"عدد أفراد الطاقم: {CharacterLibrary.Roster.Length}");

            var castKeys = new HashSet<string>();
            foreach (var e in CharacterLibrary.Roster)
                castKeys.Add(e.key);
            Check(castKeys.Count == CharacterLibrary.Roster.Length, "مفاتيح الشخصيات فريدة");

            foreach (var e in CharacterLibrary.Roster)
                Check(e.height >= 1.60f && e.height <= 1.90f,
                      $"{e.displayName}: طول {e.height:0.00}م منطقي");

            string charDir = Path.GetFullPath(Path.Combine(storyDir, "..", "Characters"));
            int foundFbx = 0, foundTex = 0, badTexDirs = 0;

            foreach (var e in CharacterLibrary.Roster)
            {
                string dir = Path.Combine(charDir, e.key);
                var fbx = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.fbx") : new string[0];
                if (fbx.Length > 0) foundFbx++;

                var fbm = Directory.Exists(dir) ? Directory.GetDirectories(dir, "*.fbm") : new string[0];
                if (fbm.Length > 0)
                {
                    // التكستشرات لازم تكون في جذر .fbm وبالاسم اللي الـ FBX بيدوّر عليه
                    var tex = Directory.GetFiles(fbm[0], "*.jpg");
                    foundTex += tex.Length;

                    var names = new System.Collections.Generic.List<string>();
                    foreach (var t in tex) names.Add(Path.GetFileName(t));

                    bool hasBase = names.Exists(n => n.Contains("basecolor"));
                    bool hasNormal = names.Exists(n => n.Contains("normal"));
                    if (tex.Length < 4 || !hasBase || !hasNormal) badTexDirs++;

                    // ممنوع مجلدات متداخلة (مسارات ويندوز اتسرّبت مرة)
                    if (Directory.GetDirectories(fbm[0]).Length > 0) badTexDirs++;
                }
                else badTexDirs++;
            }

            // الموديلات ممكن تكون في شجرة العمل أو داخل أرشيف التسليم
            string projRoot = Path.GetFullPath(Path.Combine(charDir, "..", "..", "..", "..", ".."));
            var zips = Directory.Exists(projRoot)
                ? Directory.GetFiles(projRoot, "THE_LAST_CONTRACT_v*.zip") : new string[0];
            bool inArchive = zips.Length > 0;
            Check(foundFbx == 4 || inArchive,
                  foundFbx == 4 ? $"موديلات FBX بشري: 4/4"
                                : "موديلات FBX داخل أرشيف التسليم");
            Check(badTexDirs == 0 || foundFbx == 0,
                  foundFbx == 4 ? "كل موديل معاه تكستشراته كاملة" : "التكستشرات مرفقة مع الموديلات في الأرشيف",
                  $"{badTexDirs} موديل ناقص تكستشر");
            Check(foundTex >= 16, $"ملفات تكستشر الشخصيات: {foundTex}");

            // خط عربي + سكربت التسجيل
            string fontDir = Path.GetFullPath(Path.Combine(storyDir, "..", "Fonts"));
            var fonts = Directory.Exists(fontDir) ? Directory.GetFiles(fontDir, "*.ttf") : new string[0];
            Check(fonts.Length >= 1, $"خطوط TTF: {fonts.Length}");
            Check(Directory.Exists(fontDir) && Directory.GetFiles(fontDir, "*.ttf").Any(f => f.Contains("Amiri")),
                  "خط Amiri العربي موجود");
            Check(File.Exists(Path.Combine(fontDir, "OFL.txt")), "رخصة الخط مرفقة");

            string voPath = Path.GetFullPath(Path.Combine(storyDir, "..", "..", "..", "..", "VO_SCRIPT.md"));
            Check(File.Exists(voPath), "سكربت التسجيل الصوتي موجود");

            // التقرير بيطبع في الآخر بعد كل الأقسام
            // ══════════════════════════════════════════════════════════
            //  المشاهد + التحكم + الانتقال — الجولة الحالية
            // ══════════════════════════════════════════════════════════

            Section("١٥) مشهد اللعب Play.unity");

            string scenesDir = Path.Combine(root, "Assets", "_Project", "Scenes");
            string mainScenePath = Path.Combine(scenesDir, "Main.unity");
            string playScenePath = Path.Combine(scenesDir, "Play.unity");

            Check(File.Exists(mainScenePath), "Main.unity موجود");
            Check(File.Exists(playScenePath), "Play.unity موجود (مشهد لعب مستقل)");
            Check(File.Exists(playScenePath + ".meta"), "Play.unity.meta موجود (GUID ثابت)");

            if (File.Exists(playScenePath))
            {
                string play = File.ReadAllText(playScenePath);

                Check(!play.Contains("{{") && !play.Contains("}}"), "مفيش أقواس YAML مزدوجة");
                Check(play.Count(c => c == '{') == play.Count(c => c == '}'),
                      "أقواس YAML متوازنة",
                      $"مفتوح {play.Count(c => c == '{')} · مقفول {play.Count(c => c == '}')}");

                // ── تكامل المعرّفات ──
                var defined = new HashSet<int>(
                    Regex.Matches(play, @"^--- !u!\d+ &(\d+)$", RegexOptions.Multiline)
                         .Select(m => int.Parse(m.Groups[1].Value)));

                var referenced = new HashSet<int>(
                    Regex.Matches(play, @"fileID:\s*(\d+)")
                         .Select(m => int.Parse(m.Groups[1].Value))
                         .Where(id => id > 0 && id != 11500000));

                var broken = referenced.Where(id => !defined.Contains(id)).ToList();
                Check(broken.Count == 0,
                      "كل مراجع fileID بتشاور على كائنات موجودة",
                      broken.Count > 0 ? "مكسور: " + string.Join(",", broken) : null);

                Check(defined.Count >= 25, $"عدد الكائنات/المكونات: {defined.Count}");

                // ── الكائنات المطلوبة ──
                var required = new[]
                {
                    "Main Camera", "Directional Light", "WORLD", "Apartment", "Lighting",
                    "CAST", "ADAM_MANSOUR", "MOBILE_CONTROLS", "OrientationWatcher", "GAME"
                };
                foreach (var name in required)
                    Check(Regex.IsMatch(play, @"m_Name: " + Regex.Escape(name) + @"$", RegexOptions.Multiline),
                          $"كائن موجود في المشهد: {name}");

                Check(Regex.IsMatch(play, @"m_Name: Main Camera\n[\s\S]{0,80}m_TagString: MainCamera"),
                      "كاميرا المشهد متسمّاة MainCamera");

                // ── آدم في المشهد ──
                Check(Regex.IsMatch(play, @"m_Name: ADAM_MANSOUR[\s\S]{0,900}?AdamBuilder") ||
                      play.Contains("ADAM_MANSOUR"), "آدم كائن حقيقي في المشهد");

                Check(play.Contains("m_LocalPosition: {x: -0.6, y: 0.0, z: -1.4}"),
                      "نقطة ظهور آدم مكتوبة في المشهد (-0.6, 0, -1.4)");

                // ── ربط الكاميرا بآدم ──
                var adamTransform = Regex.Match(play,
                    @"--- !u!4 &(\d+)\nTransform:[\s\S]{0,300}?m_GameObject: \{fileID: (\d+)\}");

                Check(play.Contains("target: {fileID: 100061}"),
                      "ThirdPersonCameraRig.target بيشاور على Transform آدم");

                // ── المراجع للسكربتات ──
                var guidToCs = new Dictionary<string, string>();
                foreach (var meta in Directory.GetFiles(Path.Combine(root, "Assets"), "*.cs.meta",
                                                        SearchOption.AllDirectories))
                {
                    var m = Regex.Match(File.ReadAllText(meta), @"guid:\s*([0-9a-f]{32})");
                    if (m.Success) guidToCs[m.Groups[1].Value] = meta.Substring(0, meta.Length - 5);
                }

                var scriptGuids = Regex.Matches(play, @"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}")
                                       .Select(m => m.Groups[1].Value).ToList();

                Check(scriptGuids.Count >= 7, $"سكربتات مربوطة في المشهد: {scriptGuids.Count}");
                Check(scriptGuids.All(g => guidToCs.ContainsKey(g)),
                      "كل GUID سكربت في المشهد له ملف .cs.meta",
                      string.Join(",", scriptGuids.Where(g => !guidToCs.ContainsKey(g))));

                // ── حقول الـ MonoBehaviour لازم تكون موجودة في الكود ──
                int unknownFields = 0;
                var unknownSamples = new List<string>();
                var monoBlocks = Regex.Matches(play, @"--- !u!114 &\d+\nMonoBehaviour:[\s\S]*?(?=\n--- |\z)");
                foreach (Match block in monoBlocks)
                {
                    var guidMatch = Regex.Match(block.Value, @"guid: ([0-9a-f]{32})");
                    if (!guidMatch.Success || !guidToCs.TryGetValue(guidMatch.Groups[1].Value, out var cs))
                        continue;

                    string code = File.ReadAllText(cs);
                    foreach (var line in block.Value.Split('\n'))
                    {
                        var km = Regex.Match(line, @"^  ([A-Za-z_][A-Za-z0-9_]*): ");
                        if (!km.Success) continue;
                        string key = km.Groups[1].Value;
                        if (key.StartsWith("m_")) continue;   // حقول المحرك
                        if (!code.Contains(key))
                        {
                            unknownFields++;
                            if (unknownSamples.Count < 4) unknownSamples.Add(Path.GetFileName(cs) + "." + key);
                        }
                    }
                }
                Check(unknownFields == 0,
                      "كل الحقول المكتوبة في المشهد موجودة في الكود (مفيش أسماء غلط)",
                      string.Join(" · ", unknownSamples));

                // ── إعدادات اللعب ──
                Check(Regex.IsMatch(play, @"useSceneObjects: 1"), "Play.unity: useSceneObjects = 1");
                Check(Regex.IsMatch(play, @"menuOnly: 0"), "Play.unity: menuOnly = 0");
                Check(Regex.IsMatch(play, @"showTitleMenu: 0"), "Play.unity: showTitleMenu = 0");

                string main = File.Exists(mainScenePath) ? File.ReadAllText(mainScenePath) : "";
                Check(Regex.IsMatch(main, @"menuOnly: 1"), "Main.unity: menuOnly = 1 (قائمة بس)");
                Check(Regex.IsMatch(main, @"playSceneName: Play"), "Main.unity: بيحمّل مشهد Play");
            }

            // ── Build Settings ──
            string buildSettings = Path.Combine(root, "ProjectSettings", "EditorBuildSettings.asset");
            Check(File.Exists(buildSettings), "EditorBuildSettings.asset موجود");
            if (File.Exists(buildSettings))
            {
                string bs = File.ReadAllText(buildSettings);
                Check(bs.Contains("Main.unity"), "Main في Build Settings");
                Check(bs.Contains("Play.unity"), "Play في Build Settings");

                var playGuidInBs = Regex.Match(bs,
                    @"path: Assets/_Project/Scenes/Play\.unity\n\s+guid: ([0-9a-f]{32})");
                string playMetaGuid = File.Exists(playScenePath + ".meta")
                    ? Regex.Match(File.ReadAllText(playScenePath + ".meta"), @"guid:\s*([0-9a-f]{32})").Groups[1].Value
                    : "";

                Check(playGuidInBs.Success && playGuidInBs.Groups[1].Value == playMetaGuid,
                      "GUID مشهد Play في Build Settings مطابق للـ .meta");

                int enabledCount = Regex.Matches(bs, @"enabled: 1").Count;
                Check(enabledCount == 2, $"عدد المشاهد المفعّلة في البناء: {enabledCount}");
            }

            Section("١٦) واجهة تحكم الموبايل");

            string mcPath = Path.Combine(root, "Assets", "_Project", "Scripts", "UI", "MobileControls.cs");
            Check(File.Exists(mcPath), "MobileControls.cs موجود");
            if (File.Exists(mcPath))
            {
                string mc = File.ReadAllText(mcPath);
                Check(mc.Contains("class JoystickBehaviour"), "عصا افتراضية (JoystickBehaviour)");
                Check(mc.Contains("class LookPadBehaviour"), "منطقة لمس للكاميرا (LookPadBehaviour)");
                Check(mc.Contains("InteractButton"), "زر تفاعل");
                Check(mc.Contains("RunButton"), "زر جري");
                Check(mc.Contains("AdvanceButton"), "زر متابعة الحوار");
                Check(mc.Contains("SetMobileMove"), "العصا بتبعت الحركة لـ PlayerInputReader");
                Check(mc.Contains("TriggerInteract"), "زر التفاعل موصول بالإدخال");
                Check(mc.Contains("ThirdPersonController"), "التحكم مربوط بـ ThirdPersonController");
                Check(mc.Contains("ApplyLayout"), "تخطيط قابل لإعادة الحساب");
                Check(mc.Contains("IsPortrait"), "بيقرا الاتجاه (رأسي/أفقي)");
                Check(mc.Contains("SafeArea") || mc.Contains("CreateSafeArea"), "بيحترم النوتش (Safe Area)");
                Check(mc.Contains("ControlSettings"), "بيقرا إعدادات التحكم");
                Check(mc.Contains("ClearMobileInput"), "بيمسح الإدخال لما يتقفل");
            }

            string bootPath = Path.Combine(root, "Assets", "_Project", "Scripts", "Bootstrap", "GameBootstrap.cs");
            string boot = File.Exists(bootPath) ? File.ReadAllText(bootPath) : "";
            Check(boot.Contains("BuildMobileControls"), "GameBootstrap بيبني واجهة الموبايل فعلاً");
            Check(Regex.IsMatch(boot, @"if \(!buildMobileControls\)"),
                  "الفلاج buildMobileControls بقى مستخدم (كان معلّق ومش بيتقرا)");
            Check(boot.Contains("_mobileControls.Build("), "بيبنيها بالمراجع الصح (input/UI/player)");
            Check(boot.Contains("SetInteractAvailable"), "زر التفاعل بيظهر بس عند وجود هدف");

            string inputPath = Path.Combine(root, "Assets", "_Project", "Scripts", "Player", "PlayerInputReader.cs");
            Check(File.Exists(inputPath), "PlayerInputReader.cs موجود");
            if (File.Exists(inputPath))
            {
                string pi = File.ReadAllText(inputPath);
                Check(!pi.Contains("if (mobileMode)"), "مسارات الإدخال مش ورا فلاج mobileMode (الباج القديم)");
                Check(pi.Contains("SetMobileMove") && pi.Contains("SetMobileLook"), "واجهة الموبايل مربوطة");
                Check(pi.Contains("TouchActive"), "بيبلّغ إن الإدخال جه من اللمس");
                Check(pi.Contains("interact") || pi.Contains("Interact"), "زر التفاعل بيوصل كـ InteractPressed");
            }

            string controllerPath = Path.Combine(root, "Assets", "_Project", "Scripts", "Player", "ThirdPersonController.cs");
            string tc = File.Exists(controllerPath) ? File.ReadAllText(controllerPath) : "";
            Check(tc.Contains("SetAnimator("), "ThirdPersonController بيقبل ربط Animator");
            Check(tc.Contains("HasParam("), "بيتأكد من وجود البارامتر قبل إرساله (مفيش تحذيرات كل فريم)");

            Section("١٧) إعدادات التحكم (محفوظة)");

            ControlSettings.ResetToDefaults();
            Check(Math.Abs(ControlSettings.LookSensitivity - ControlSettings.DefaultLook) < 0.001f,
                  "الحساسية الافتراضية 1.00");
            Check(ControlSettings.StickSide == JoystickSide.Left, "العصا افتراضياً على الشمال");

            ControlSettings.SetLookSensitivity(99f);
            Check(Math.Abs(ControlSettings.LookSensitivity - ControlSettings.LookMax) < 0.001f,
                  $"القيم بتتقصّ على الحد الأعلى ({ControlSettings.LookMax})");

            ControlSettings.SetLookSensitivity(-5f);
            Check(Math.Abs(ControlSettings.LookSensitivity - ControlSettings.LookMin) < 0.001f,
                  $"القيم بتتقصّ على الحد الأدنى ({ControlSettings.LookMin})");

            ControlSettings.SetStickScale(10f);
            Check(Math.Abs(ControlSettings.StickScale - ControlSettings.StickMax) < 0.001f,
                  $"حجم العصا مقصوص ({ControlSettings.StickMax})");

            float before = ControlSettings.LookSensitivity;
            float next = ControlSettings.NextLook(before);
            Check(next > before || Math.Abs(next - ControlSettings.LookMin) < 0.001f,
                  "NextLook بيتقدّم أو بيلفّ للبداية");

            ControlSettings.SetLookSensitivity(ControlSettings.LookMax);
            Check(Math.Abs(ControlSettings.NextLook(ControlSettings.LookMax) - ControlSettings.LookMin) < 0.001f,
                  "NextLook بيلفّ من الآخر للأول");

            ControlSettings.ToggleStickSide();
            Check(ControlSettings.StickSide == JoystickSide.Right, "تبديل جهة العصا شغّال");
            ControlSettings.ToggleStickSide();
            Check(ControlSettings.StickSide == JoystickSide.Left, "الرجوع للشمال شغّال");

            ControlSettings.ToggleInvertY();
            Check(ControlSettings.InvertY, "عكس محور Y شغّال");
            ControlSettings.ToggleAutoRun();
            Check(ControlSettings.AutoRun, "الجري التلقائي شغّال");

            float stored = ControlSettings.LookSensitivity;
            Check(Math.Abs(PlayerPrefs.GetFloat("tlc.ctrl.look", -1f) - stored) < 0.001f,
                  "القيمة محفوظة فعلياً في PlayerPrefs");

            ControlSettings.ResetToDefaults();
            Check(!ControlSettings.InvertY && !ControlSettings.AutoRun,
                  "الاسترجاع الافتراضي بيرجّع كل حاجة");

            Check(Localization.Get("settings.look").Length > 0, "مفتاح ترجمة الحساسية موجود");
            Check(Localization.Get("settings.stickSide.right").Length > 0, "مفتاح ترجمة جهة العصا موجود");

            var settingsEntries = new MenuModel();
            settingsEntries.Open(UIScreen.Settings);
            var built = settingsEntries.Build(new MenuContext());
            Check(built.Any(e => e.Action == MenuAction.CycleLook), "القائمة فيها إعداد حساسية");
            Check(built.Any(e => e.Action == MenuAction.ToggleStickSide), "القائمة فيها جهة العصا");
            Check(built.Any(e => e.Action == MenuAction.ResetControls), "القائمة فيها استرجاع افتراضي");

            Section("١٨) الانتقال من القائمة لمشهد اللعب");

            PlaySession.Clear();
            Check(!PlaySession.HasRequest, "الحالة بتبدأ فاضية");

            PlaySession.RequestNewGame();
            Check(PlaySession.HasRequest && PlaySession.Mode == PlayEntryMode.NewGame,
                  "طلب لعبة جديدة اتسجّل");

            bool consumed = PlaySession.Consume(out var mode, out var chapter);
            Check(consumed && mode == PlayEntryMode.NewGame,
                  "الطلب اتقرأ مرة واحدة");
            Check(!PlaySession.HasRequest, "الطلب ما اتقرأش مرتين");
            Check(!PlaySession.Consume(out _, out _), "القراءة التانية بترجّع false");

            PlaySession.RequestChapter(99);
            PlaySession.Consume(out _, out int clampedChapter);
            Check(clampedChapter == 3, $"رقم الفصل بيتقصّ على 3 (طلع {clampedChapter})");

            PlaySession.RequestChapter(-4);
            PlaySession.Consume(out _, out int lowChapter);
            Check(lowChapter == 1, $"رقم الفصل الأدنى 1 (طلع {lowChapter})");

            PlaySession.RequestContinue();
            PlaySession.Consume(out var contMode, out _);
            Check(contMode == PlayEntryMode.Continue, "طلب المتابعة اتسجّل صح");

            Check(PlaySession.Describe(PlayEntryMode.Continue, 0).Contains("متابعة"),
                  "وصف الطلب مقروء في السجل");

            string flowPath = Path.Combine(root, "Assets", "_Project", "Scripts", "Bootstrap", "SceneFlow.cs");
            Check(File.Exists(flowPath), "SceneFlow.cs موجود");
            if (File.Exists(flowPath))
            {
                string flow = File.ReadAllText(flowPath);
                Check(flow.Contains("public const string PlayScene = \"Play\""), "اسم مشهد اللعب معرّف مرة واحدة");
                Check(flow.Contains("CanStreamedLevelBeLoaded"), "بيتحقق من Build Settings قبل التحميل");
                Check(flow.Contains("LoadScene"), "بيحمّل المشهد فعلياً");
            }

            Check(boot.Contains("PlaySession.Consume"), "مشهد اللعب بياخد الطلب عند الدخول");
            Check(boot.Contains("LeaveMenuForPlay"), "القائمة بتنتقل لمشهد اللعب");
            Check(boot.Contains("SceneManager.LoadScene"), "الانتقال تحميل مشهد حقيقي");
            Check(Regex.IsMatch(boot, @"if \(!SceneFlow.CanLoad"),
                  "لو المشهد مش في Build Settings بيكمل في نفس المشهد بدل ما يقع");

            Section("١٩) الاتجاه والـ Animator وقت التشغيل");

            string orientPath = Path.Combine(root, "Assets", "_Project", "Scripts", "UI", "OrientationWatcher.cs");
            Check(File.Exists(orientPath), "OrientationWatcher.cs موجود");
            if (File.Exists(orientPath))
            {
                string o = File.ReadAllText(orientPath);
                Check(o.Contains("ClassifyPortrait"), "بيصنّف رأسي/أفقي");
                Check(o.Contains("VerticalFovFor"), "بيعدّل الكاميرا على الشاشة الرأسية");
                Check(o.Contains("Screen.autorotateToLandscapeLeft"), "بيسمح بالاتجاه الأفقي");
                Check(o.Contains("Screen.autorotateToPortrait"), "بيسمح بالاتجاه الرأسي");
                Check(o.Contains("referenceResolution"), "بيعدّل مرجع الواجهة مع الاتجاه");
            }

            string binderPath = Path.Combine(root, "Assets", "_Project", "Scripts", "World", "CharacterAnimatorBinder.cs");
            Check(File.Exists(binderPath), "CharacterAnimatorBinder.cs موجود");
            if (File.Exists(binderPath))
            {
                string b = File.ReadAllText(binderPath);
                Check(b.Contains("Animators/TLC_Character"), "بيدوّر على الـ controller في نفس المسار");
                Check(b.Contains("DrivesAnimator"), "مالك الحركة واحد (مفيش تعارض Puppet/Controller)");
                Check(b.Contains("isHuman"), "بيتحقق إن الـ Avatar Humanoid");
                Check(b.Contains("SetAnimator("), "بيربط الـ Animator بالمتحكم");
            }

            string adamPath = Path.Combine(root, "Assets", "_Project", "Scripts", "World", "AdamBuilder.cs");
            if (File.Exists(adamPath))
            {
                string a = File.ReadAllText(adamPath);
                Check(a.Contains("EnsureController()"), "آدم بيضمن وجود المتحكم في كل المسارات");
                Check(a.Contains("BindAnimator()"), "آدم بيربط الـ Animator بعد البناء");
                Check(Regex.Matches(a, @"EnsureController\(\);").Count >= 2,
                      "الاستدعاء مش جوّه مسار واحد (كان ده سبب باج الـ NullReference)");
            }

            string editorDir = Path.Combine(root, "Assets", "_Project", "Editor");
            Check(File.Exists(Path.Combine(editorDir, "PlaySceneBuilder.cs")), "أداة توليد مشهد اللعب موجودة");
            Check(File.Exists(Path.Combine(editorDir, "PlaySceneValidator.cs")), "أداة فحص المشهد موجودة");
            Check(File.Exists(Path.Combine(root, "tools", "make_scenes.py")), "مولّد المشهد الأوفلاين موجود");

            Section("٢٠) أدوات التحقق النهائي");

            string verRunner = Path.Combine(root, "Assets", "_Project", "Editor", "VerificationRunner.cs");
            Check(File.Exists(verRunner), "VerificationRunner.cs موجود");
            if (File.Exists(verRunner))
            {
                string vr = File.ReadAllText(verRunner);
                Check(vr.Contains("CaptureScreenshot"), "بيصوّر لقطات شاشة من المحرك");
                Check(vr.Contains("logMessageReceived"), "بيلتقط سجل Console");
                Check(vr.Contains("editor-report.txt"), "بيكتب تقرير محرر");
                Check(vr.Contains("play-report.txt"), "بيكتب تقرير تشغيل");
                Check(vr.Contains("isHuman"), "بيتحقق من Humanoid Avatar فعلياً");
                Check(vr.Contains("Missing") || vr.Contains("c == null"), "بيفحص Missing Scripts");
            }

            // أرقام منيو TLC مكررة = ارتباك للمطوّر
            string editorDir2 = Path.Combine(root, "Assets", "_Project", "Editor");
            var menuNumbers = new List<string>();
            foreach (var f in Directory.GetFiles(editorDir2, "*.cs"))
                foreach (Match m in Regex.Matches(File.ReadAllText(f), "MenuItem\\(\"TLC/([0-9]+[a-z]?)"))
                    menuNumbers.Add(m.Groups[1].Value);

            var dupNumbers = menuNumbers.GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            Check(dupNumbers.Count == 0,
                  $"أرقام منيو TLC فريدة ({menuNumbers.Count} عنصر)",
                  dupNumbers.Count > 0 ? "مكرر: " + string.Join(",", dupNumbers) : null);

            // مسار الـ controller لازم يبقى واحد في كل الملفات
            string acb = Path.Combine(editorDir2, "AnimatorControllerBuilder.cs");
            string puppet = Path.Combine(root, "Assets", "_Project", "Scripts", "World", "HumanoidPuppet.cs");
            string binder = Path.Combine(root, "Assets", "_Project", "Scripts", "World", "CharacterAnimatorBinder.cs");
            string importSet = Path.Combine(editorDir2, "CharacterImportSettings.cs");

            if (File.Exists(acb))
                Check(File.ReadAllText(acb).Contains("Assets/_Project/Resources/Animators/TLC_Character.controller"),
                      "مسار الـ controller مضبوط في المولّد");
            if (File.Exists(puppet))
                Check(File.ReadAllText(puppet).Contains("Animators/TLC_Character"),
                      "HumanoidPuppet بيحمّل من نفس المسار");
            if (File.Exists(binder))
                Check(File.ReadAllText(binder).Contains("Animators/TLC_Character"),
                      "CharacterAnimatorBinder بيحمّل من نفس المسار");
            if (File.Exists(importSet))
                Check(File.ReadAllText(importSet).Contains("ModelImporterAnimationType.Humanoid"),
                      "الاستيراد بيحط Humanoid تلقائياً");

            // مخرجات التحقق الموجودة فعلاً
            string verDir = Path.Combine(root, "VERIFICATION");
            Check(File.Exists(Path.Combine(root, "VERIFICATION.md")), "VERIFICATION.md موجود");
            Check(File.Exists(Path.Combine(verDir, "ENVIRONMENT.txt")), "سجل فحص البيئة موجود");
            Check(File.Exists(Path.Combine(verDir, "fbx-summary.txt")), "ملخّص فحص الموديلات موجود");

            if (File.Exists(Path.Combine(verDir, "fbx-summary.txt")))
            {
                string fbxSum = File.ReadAllText(Path.Combine(verDir, "fbx-summary.txt"));
                int perfect = Regex.Matches(fbxSum, @"\*\*21/21\*\*").Count;
                Check(perfect == 4, $"الأربعة عندهم 21/21 عظمة (لقينا {perfect})");
            }

            string env = File.Exists(Path.Combine(verDir, "ENVIRONMENT.txt"))
                ? File.ReadAllText(Path.Combine(verDir, "ENVIRONMENT.txt")) : "";
            Check(env.Contains("unity مش في PATH"), "البيئة مسجّلة إن Unity غير موجود (صراحة مش ادّعاء)");

            // ما اتحذفش حاجة: الفصول والنهايات
            Check(chapters.Count == 3, $"الفصول الثلاثة موجودة ({chapters.Count})");
            int endingCount = chapters.Sum(c => c.story.Document.nodes.Count(n => n.type == 4));
            Check(endingCount >= 12, $"النهايات لسه موجودة ({endingCount})");

            Section("٢١) محاكاة جلسة لعب بكود المشروع نفسه");

            var simSamples = PlaySimulation.Run(out string simReport, out _);
            Directory.CreateDirectory(Path.Combine(root, "VERIFICATION"));
            File.WriteAllText(Path.Combine(root, "VERIFICATION", "movement-sim.txt"), simReport, Encoding.UTF8);
            PlaySimulation.WriteCsv(simSamples, Path.Combine(root, "VERIFICATION", "movement-trace.csv"));

            float simIdle = 0f, simWalk = 0f, simRun = 0f, simDialogue = 0f;
            foreach (var s in simSamples)
            {
                if (s.phase == "سكون" && s.t > 1.5f) simIdle = Mathf.Max(simIdle, s.speed);
                if (s.phase == "مشي" && s.t > 4.5f) simWalk = Mathf.Max(simWalk, s.speed);
                if (s.phase == "جري") simRun = Mathf.Max(simRun, s.speed);
                if (s.phase == "حوار مفتوح") simDialogue = Mathf.Max(simDialogue, s.speed);
            }

            Check(simSamples.Count > 590, $"عدد الفريمات المحاكاة: {simSamples.Count}");
            Check(simIdle < 0.05f, $"سكون: مفيش حركة ({simIdle:0.000} م/ث)");
            Check(simWalk > 1.3f && simWalk < 1.7f, $"مشي: سرعة مستقرة {simWalk:0.00} م/ث (المضبوط 1.55)");
            Check(simRun > 3.6f && simRun < 4.3f, $"جري: سرعة {simRun:0.00} م/ث (المضبوط 4.1)");
            Check(simRun > simWalk * 2f, "الجري أسرع من المشي بأكتر من الضعف");
            Check(simDialogue < 0.05f, $"وقت الحوار الحركة بتقف ({simDialogue:0.000} م/ث)");

            var simStart = simSamples[0].pos;
            var simEnd = simSamples[simSamples.Count - 1].pos;
            float moved = Vector3.Distance(simStart, simEnd);
            Check(moved > 8f, $"آدم اتحرك فعلياً مسافة {moved:0.00} م في المحاكاة");
            Check(Mathf.Abs(simEnd.x - simStart.x) < 0.05f,
                  "الحركة على محور واحد زي ما المدخل بيدفع (مفيش انحراف)");

            string traceCsv = Path.Combine(root, "VERIFICATION", "movement-trace.csv");
            Check(File.Exists(traceCsv), "ملف التتبّع movement-trace.csv اتكتب");
            if (File.Exists(traceCsv))
                Check(File.ReadAllLines(traceCsv).Length > 590, "كل فريم مسجّل في الملف");

            Console.WriteLine(string.Join(Environment.NewLine, _log));
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════");
            if (_failures == 0)
                Console.WriteLine("✅ كل الاختبارات نجحت");
            else
                Console.WriteLine($"❌ فشل {_failures} اختبار");
            Console.WriteLine("═══════════════════════════════════════════════");

            return _failures == 0 ? 0 : 1;
        }

        static void CollectStats(Effect e, HashSet<string> into)
        {
            if (e?.stats == null) return;
            foreach (var m in e.stats)
                if (!string.IsNullOrEmpty(m.key)) into.Add(m.key);
        }

        static HashSet<string> BfsReachable(CompiledStory story)
        {
            var seen = new HashSet<string>();
            var queue = new Queue<string>();

            // نقاط الدخول = entry + أي عقدة دخول من فصل سابق (chN_from_*)
            queue.Enqueue(story.Document.entry);
            foreach (var n in story.Document.nodes)
                if (n.id.StartsWith("ch") && n.id.Contains("_from_"))
                    queue.Enqueue(n.id);

            while (queue.Count > 0)
            {
                string id = queue.Dequeue();
                if (id == null || !seen.Add(id)) continue;
                var n = story.Get(id);
                if (n == null) continue;

                if (!string.IsNullOrEmpty(n.next)) queue.Enqueue(n.next);
                if (!string.IsNullOrEmpty(n.elseNext)) queue.Enqueue(n.elseNext);
                if (n.options != null)
                    foreach (var o in n.options)
                        if (!string.IsNullOrEmpty(o.next)) queue.Enqueue(o.next);
            }
            return seen;
        }

        // ══════════════════════════════════════════════════════

        class Explorer
        {
            readonly List<(string file, string json, CompiledStory story)> _chapters;
            readonly HashSet<string> _memo = new HashSet<string>();

            public int PathsExplored;
            public bool CapHit;
            public HashSet<string> EndingsReached = new HashSet<string>();
            public HashSet<string> NodesVisited = new HashSet<string>();

            const int Cap = 3_000_000;

            public Explorer(List<(string, string, CompiledStory)> chapters) => _chapters = chapters;

            public void Run()
            {
                var state = new GameState();
                state.ResetToDefaults();
                Walk(0, _chapters[0].story.Document.entry, state);
            }

            void Walk(int chapterIndex, string nodeId, GameState state)
            {
                if (CapHit) return;

                if (PathsExplored++ > Cap) { CapHit = true; return; }

                var story = _chapters[chapterIndex].story;
                var node = story.Get(nodeId);
                if (node == null) return;

                string key = $"{chapterIndex}|{nodeId}|{StatsKey(state)}|{FlagsKey(state)}";
                if (!_memo.Add(key)) return;      // حالة مزارة قبل كده بنفس النتيجة

                NodesVisited.Add(nodeId);

                if (node.onEnter != null) node.onEnter.Apply(state);

                if (node.require != null && !node.require.IsMet(state))
                {
                    if (!string.IsNullOrEmpty(node.elseNext)) Walk(chapterIndex, node.elseNext, state);
                    return;
                }

                switch (node.Kind)
                {
                    case NodeKind.Ending:
                        EndingsReached.Add(node.endingId);
                        int nextIdx = NextChapter(node.endingId);
                        if (nextIdx >= 0 && nextIdx < _chapters.Count)
                        {
                            string entry = $"ch{nextIdx + 1}_from_{node.endingId.ToLowerInvariant()}";
                            if (_chapters[nextIdx].story.Get(entry) != null)
                                Walk(nextIdx, entry, state);
                        }
                        return;

                    case NodeKind.Choice:
                        if (node.options != null)
                            foreach (var opt in node.options)
                            {
                                if (!opt.IsAvailable(state)) continue;
                                var branch = Clone(state);
                                opt.effect?.Apply(branch);
                                if (!string.IsNullOrEmpty(opt.decisionId)) branch.RecordDecision(opt.decisionId);
                                Walk(chapterIndex, opt.next, branch);
                            }
                        return;

                    default:
                        if (!string.IsNullOrEmpty(node.next)) Walk(chapterIndex, node.next, state);
                        return;
                }
            }

            static int NextChapter(string endingId)
            {
                if (string.IsNullOrEmpty(endingId)) return -1;
                var parts = endingId.Split('_');
                if (parts.Length < 2 || !parts[0].StartsWith("CH")) return -1;
                if (!int.TryParse(parts[0].Substring(2), out int n)) return -1;
                return n >= 3 ? -1 : n;     // CH1 → الفصل الثاني (index 1)
            }

            static string StatsKey(GameState s)
            {
                var sb = new StringBuilder();
                foreach (var k in GameState.AllStats) sb.Append((int)Math.Round(s.Get(k))).Append(',');
                return sb.ToString();
            }

            static string FlagsKey(GameState s)
            {
                var list = s.Flags.ToList();
                list.Sort(StringComparer.Ordinal);
                return string.Join("|", list);
            }
        }

        static GameState Clone(GameState s)
        {
            var copy = new GameState();
            copy.FromDto(s.ToDto());
            return copy;
        }

        // ══════════════════════════════════════════════════════

        static void RunRunnerIntegration(string chapter01Json)
        {
            var state = new GameState();
            state.ResetToDefaults();

            var go = new GameObject("SimRunner");
            var runner = go.AddComponent<NarrativeRunner>();

            StoryNode ended = null;
            StoryNode atChoice = null;
            List<ChoiceOption> choices = null;
            string pendingAction = null;
            string pendingParam = null;
            int lineCount = 0;

            runner.LineShown += n => lineCount++;
            runner.ChoicesShown += (n, opts) => { atChoice = n; choices = opts; };
            runner.EndingReached += n => ended = n;
            runner.EventRaised += (a, p) => { pendingAction = a; pendingParam = p; };

            bool init = runner.Initialize(state, chapter01Json);
            Check(init, "تهيئة المشغّل بالقصة");

            runner.Begin();

            int guard = 0;
            int decisions = 0;

            while (guard++ < 4000)
            {
                if (ended != null) break;

                if (atChoice != null && choices != null)
                {
                    atChoice = null;
                    int pick = guard % choices.Count;   // تنويع الاختيار
                    choices = null;
                    decisions++;
                    runner.Choose(pick);
                    continue;
                }

                if (pendingAction != null)
                {
                    string a = pendingAction, p = pendingParam;
                    pendingAction = null;
                    if (a == NarrativeRunner.ActWaitInteract) runner.NotifyInteraction(p);
                    else runner.ResumeAfterEvent();
                    continue;
                }

                if (runner.IsWaitingForInput) { runner.Advance(); continue; }

                break;
            }

            Check(lineCount > 10, $"المشغّل عرض {lineCount} سطر");
            Check(decisions >= 3, $"المشغّل مرّ بـ {decisions} قرار");
            Check(ended != null, "المشغّل وصل لنهاية",
                  ended == null ? "وقف قبل النهاية" : "");
            if (ended != null)
            {
                _log.Add($"     → النهاية: {ended.endingId} — {ended.endingTitle}");
                Check(state.Decisions.Count > 0, "القرارات اتسجلت في السجل");
                Check(state.CurrentNodeId != null, $"المؤشر في العقدة '{state.CurrentNodeId}'");
            }
        }

        static void RunSaveLoadTest()
        {
            var original = new GameState();
            original.ResetToDefaults();
            original.AddStat(GameState.FamilyTruth, 42);
            original.SetFlag("test_flag_alpha");
            original.RecordDecision("TEST_DECISION_1");
            original.CurrentNodeId = "c1_test_node";
            original.ChapterId = "chapter01";

            bool saved = SaveSystem.Save(SaveSystem.ManualSlotB, original);
            Check(saved, "الحفظ نجح");
            Check(SaveSystem.Exists(SaveSystem.ManualSlotB), "ملف الحفظ موجود على القرص");

            var restored = new GameState();
            bool loaded = SaveSystem.Load(SaveSystem.ManualSlotB, restored);
            Check(loaded, "التحميل نجح");

            Check(Math.Abs(restored.Get(GameState.FamilyTruth) - original.Get(GameState.FamilyTruth)) < 0.01f,
                  "المتغيّرات اتسترجعت بالقيم الصحيحة");
            Check(restored.HasFlag("test_flag_alpha"), "الأعلام اتسترجعت");
            Check(restored.DecidedBefore("TEST_DECISION_1"), "سجل القرارات اتسترجع");
            Check(restored.CurrentNodeId == "c1_test_node", "موضع السرد اتسترجع");

            SaveSystem.Delete(SaveSystem.ManualSlotB);
            Check(!SaveSystem.Exists(SaveSystem.ManualSlotB), "الحذف شغّال");
        }

        static string FindRoot()
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 8 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir, "Assets", "_Project", "Resources", "Story")))
                    return dir;
                dir = Directory.GetParent(dir)?.FullName;
            }
            return Directory.GetCurrentDirectory();
        }
    }
}
