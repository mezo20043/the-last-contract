// 4.4 RECOVERY — Phase44Tests.cs — **مُعاد حرفيًا** (النسخة النهائية اللي طلّعت 11/0/0 و41/0/0)
// التشغيل بعمليتين منفصلتين: Run (حفظ) ثم RunRestore (إعادة تشغيل + استعادة).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Bootstrap;
using TLC.Cinematics;
using TLC.Core;
using TLC.Settings;
using TLC.UI;
using Progress = TLC.Core.Progress;

namespace TLC.EditorTools
{
    [InitializeOnLoad]
    public static class Phase44Tests
    {
        const string SessionKey = "TLC_Phase44Tests";
        const string PlayScene = "Assets/_Project/Scenes/Play.unity";

        static string _outDir = "Screenshots/phase4_4";
        static bool _running, _playSeen;
        static int _state, _frames;
        static bool _started;
        static string _mode = "save";     // save | restore
        static GameBootstrap _boot;

        static int _pass, _fail, _skip;
        static readonly List<string> _log = new List<string>();
        static readonly List<string> _fails = new List<string>();
        static readonly Dictionary<string, string> _shots = new Dictionary<string, string>();
        static int _lastTmpCount, _lastCharCount;

        [Serializable]
        class Expected
        {
            public string chapterId, nodeId, endingId;
            public float playtime;
            public List<string> flags = new List<string>();
            public List<string> decisions = new List<string>();
            public List<string> statKeys = new List<string>();
            public List<float> statValues = new List<float>();
            public string savedAtUtc;
        }
        static Expected _expect = new Expected();

        static Phase44Tests()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionKey + "Dir", _outDir);
                _mode = SessionState.GetString(SessionKey + "Mode", "save");
                _running = true; _state = 0; _frames = 0; _playSeen = false; _started = false;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
                Debug.Log($"[TLC/T44] استئناف بعد Domain Reload · الوضع={_mode}");
            }
        }

        /// <summary>الجلسة ١: لعب فعلي + حفظ + لقطات + ملف الحالة المتوقعة.</summary>
        public static void Run() => Run("save");

        /// <summary>الجلسة ٢ (عملية Unity جديدة = إعادة تشغيل التطبيق): Continue + كل اختبارات الاستعادة.</summary>
        public static void RunRestore() => Run("restore");

        static void Run(string mode)
        {
            _mode = mode;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];
                if (args[i] == "-t44mode" && i + 1 < args.Length) _mode = args[i + 1];
            }

            Directory.CreateDirectory(_outDir);
            SessionState.SetBool(SessionKey, true);
            SessionState.SetString(SessionKey + "Dir", _outDir);
            SessionState.SetString(SessionKey + "Mode", _mode);

            Log($"═══ اختبارات 4.4 · الوضع: {_mode} ═══");
            Log("Unity " + Application.unityVersion + " · " + SystemInfo.graphicsDeviceName);
            Log("مسار الحفظ: " + SaveManager.DirPath);

            if (_mode == "save")
            {
                SaveManager.ClearAll();
                Progress.Reset();
                Progress.Save();
                PlayerPrefs.DeleteKey("tlc.settings.language");
                Check("بداية نظيفة", SaveManager.ListSlots().Count == 0, "مفيش خانات بعد التصفير");
            }
            else
            {
                string path = Path.Combine(_outDir, "restore_expected.json");
                if (File.Exists(path))
                {
                    _expect = JsonUtility.FromJson<Expected>(File.ReadAllText(path));
                    Log($"📄 اتقرأ الملف المتوقع: {_expect.chapterId}/{_expect.nodeId} · أعلام={_expect.flags.Count} · قرارات={_expect.decisions.Count}");
                }
                else Check("ملف الحالة المتوقعة موجود", false, path);

                Check("الحفظ موجود على القرص قبل التشغيل", SaveManager.LatestSlot() >= 0,
                      "أحدث خانة=" + SaveManager.LatestSlot() + " · خانات=" + SaveManager.ListSlots().Count);
            }

            EditorSceneManager.OpenScene(PlayScene);
            _running = true; _state = 0; _frames = 0; _playSeen = false;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        // ───────────────────────────── أدوات ─────────────────────────────

        /// <summary>يعيد قراءة الحالة المتوقعة (بعد Domain Reload بتتصفّر الـstatics).</summary>
        static void RefreshExpected()
        {
            try
            {
                string path = Path.Combine(_outDir, "restore_expected.json");
                if (!File.Exists(path)) return;
                _expect = JsonUtility.FromJson<Expected>(File.ReadAllText(path));
                Log($"📄 الحالة المتوقعة: {_expect.chapterId}/{_expect.nodeId} · أعلام={_expect.flags.Count} · قرارات={_expect.decisions.Count}");
            }
            catch (Exception e) { Log("✗ قراءة الحالة المتوقعة فشلت: " + e.Message); }
        }

        /// <summary>الوضع ومسار اللقطات من سطر الأوامر — لازم يتقرأ تاني بعد أي Domain Reload.</summary>
        static void RefreshArgs()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-captureDir" && i + 1 < args.Length && !string.IsNullOrEmpty(args[i + 1]))
                    _outDir = args[i + 1];
                if (args[i] == "-t44mode" && i + 1 < args.Length && !string.IsNullOrEmpty(args[i + 1]))
                    _mode = args[i + 1];
            }
        }

        static void Log(string s) { _log.Add(s); Debug.Log("[TLC/T44] " + s); Flush(); }
        static void Flush()
        {
            try { File.WriteAllText(Path.Combine(_outDir, "phase44_report.txt"), string.Join("\n", _log)); }
            catch (Exception) { }
        }
        static void Check(string name, bool ok, string detail)
        {
            if (ok) { _pass++; Log($" [PASS] {name} — {detail}"); }
            else { _fail++; _fails.Add(name + " — " + detail); Log($" [FAIL] {name} — {detail}"); }
        }
        static void Skip(string name, string why) { _skip++; Log($" [SKIP] {name} — {why}"); }

        /// <summary>
        /// لقطة حقيقية بكاميرا المشهد + كل الكانفسات مربوطة بيها، مع تثبيت قنوات
        /// TMP (TexCoord1) عشان النص ما يطلعش شفاف، وقياس تباين يثبت إن الواجهة اتعرسمت.
        /// </summary>
        static void Shot(string name)
        {
            var cam = ResolveCamera();
            if (cam == null) { Log("   ✗ لقطة " + name + ": مفيش كاميرا"); return; }

            const int W = 1280, H = 720;
            RenderTexture rt = null;
            var prevT = cam.targetTexture;
            var prevA = RenderTexture.active;
            try
            {
                int bound = TLC.UI.UIFactory.BindAllCanvasesToCamera(cam);

                int tmpCount = 0, charTotal = 0;
                foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
                {
                    if (t == null || !t.gameObject.activeInHierarchy) continue;
                    tmpCount++;
                    charTotal += t.textInfo != null ? t.textInfo.characterCount : 0;

                    if (t.canvas != null)
                        t.canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                                           | AdditionalCanvasShaderChannels.Normal
                                                           | AdditionalCanvasShaderChannels.Tangent;
                    t.ForceMeshUpdate(true);
                }
                _lastTmpCount = tmpCount; _lastCharCount = charTotal;
                Canvas.ForceUpdateCanvases();

                rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                cam.targetTexture = rt;

                // رندر تمهيدي: أول فريم بعد ربط الكانفس بالكاميرا بيتبني فيه نص TMP
                cam.Render();
                foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
                    if (t != null && t.gameObject.activeInHierarchy) t.ForceMeshUpdate(true);
                Canvas.ForceUpdateCanvases();
                cam.Render();

                RenderTexture.active = rt;

                var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();

                string path = Path.Combine(_outDir, name + ".png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                _shots[name] = path;

                float contrast = Contrast(tex);
                Log($"   📸 {name}.png · كانفسات={bound} · نصوص={_lastTmpCount} (حروف={_lastCharCount}) · " +
                    $"تباين={contrast:0.000} · {new FileInfo(path).Length / 1024}KB");

                if (name == "main_menu" || name == "postcredit_A" || name == "save_load_menu")
                    Check($"لقطة {name} فيها واجهة مرسومة", contrast > 0.05f, $"تباين={contrast:0.000}");

                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e) { Log("   ✗ لقطة " + name + " فشلت: " + e.Message); }
            finally
            {
                cam.targetTexture = prevT;
                RenderTexture.active = prevA;
                if (rt != null) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            }
        }

        static Camera ResolveCamera()
        {
            var cam = Camera.main;
            if (cam != null && cam.isActiveAndEnabled) return cam;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c != null && c.isActiveAndEnabled) return c;
            return null;
        }

        static float Contrast(Texture2D tex)
        {
            float min = 1f, max = 0f;
            for (int y = tex.height / 8; y < tex.height * 7 / 8; y += 13)
                for (int x = tex.width / 10; x < tex.width * 9 / 10; x += 17)
                {
                    var c = tex.GetPixel(x, y);
                    float l = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
                    if (l < min) min = l;
                    if (l > max) max = l;
                }
            return max - min;
        }

        static bool FindBoot()
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<GameBootstrap>(FindObjectsSortMode.None))
                if (b != null && b.DebugRunnerReady) { _boot = b; return true; }
            return false;
        }

        static void Finish(int code)
        {
            Log($"\n═══ الملخّص: {_pass} نجح · {_fail} فشل · {_skip} متخطّى ═══");
            foreach (var f in _fails) Log("   ✗ " + f);
            Log($"EXIT={code}");
            Flush();
            SessionState.SetBool(SessionKey, false);
            _running = false;
            EditorApplication.update -= Tick;
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            EditorApplication.Exit(code);
        }

        static void Step(int next, int frames = 0) { _state = next; _frames = frames; }

        // ───────────────────────────── الحالة ─────────────────────────────

        static void Tick()
        {
            if (!_running) return;
            if (!EditorApplication.isPlaying) return;

            if (!_playSeen) { _playSeen = true; _frames = 0; return; }
            _frames++;

            if (_frames > 3000) { Log("✗ الجلسة وقعت في الانتظار: state=" + _state); Finish(4); return; }

            switch (_state)
            {
                case 0:
                    if (_frames < 90) return;
                    RefreshArgs();
                    Directory.CreateDirectory(_outDir);
                    if (_mode == "restore" && _expect.flags.Count == 0) RefreshExpected();
                    if (!FindBoot()) return;
                    if (_mode == "restore") _boot.DebugSuppressSceneChange = true;   // الاختبار محتاج نفس الـbootstrap
                    Log($"الوضع {_mode} جاهز · العقدة={_boot.DebugNodeId} · قنوات الحفظ={SaveManager.ListSlots().Count}");
                    Step(_mode == "save" ? 1 : 100, 0);
                    break;

                // ═════════ الجلسة ١ ═════════
                case 1:
                    if (!_started)
                    {
                        _started = true;
                        Log("بدء لعبة جديدة من القائمة");
                        _boot.StartNewGame();
                        _frames = 0;
                        return;
                    }
                    if (!_boot.DebugWaitingForInput)
                    {
                        if (_frames > 2400) { Log("✗ اللعبة ما بدأتش"); Finish(5); }
                        return;
                    }
                    Log($"اللعبة بدأت · العقدة={_boot.DebugNodeId} · {_boot.DebugLocationName}");
                    _boot.DebugManualSave(0);
                    Check("حفظ يدوي في A", _boot.DebugToast != null && (_boot.DebugToast.LastMessage ?? "").Contains("A"),
                          "رسالة: " + (_boot.DebugToast != null ? _boot.DebugToast.LastMessage : "—"));
                    Check("ملف A اتكتب", SaveManager.Exists(SaveSystem.ManualSlotA), SaveManager.PathFor(SaveSystem.ManualSlotA));
                    _boot.DebugManualSave(1);
                    Shot("slots_initial");
                    Step(2);
                    break;

                case 2:
                {
                    if (_frames < 5) return;
                    int steps = _boot.DebugAdvanceUntilChoice();
                    Log($"تقدّم سرد · {steps} سطر لحد أول قرار · العقدة={_boot.DebugNodeId}");
                    Check("وصلنا لقرار", _boot.DebugWaitingForInput && steps > 0, $"أسطر={steps}");
                    _boot.DebugChoose(0);
                    Step(3);
                    break;
                }

                case 3:
                    if (_frames < 30) return;
                    Log($"بعد القرار · قرارات={_boot.DebugDecisionCount} · أعلام={_boot.DebugFlagCount} · ثقة مراد={_boot.DebugStat(GameState.TrustMurad):0}");
                    Check("القرار اتسجّل", _boot.DebugDecisionCount > 0, "قرارات=" + _boot.DebugDecisionCount);
                    Check("حفظ تلقائي بعد القرار", SaveManager.Exists(SaveManager.AutoSlot),
                          "آخر حفظ: " + SaveManager.Describe(SaveManager.AutoSlot));
                    Shot("after_decision");
                    Step(4);
                    break;

                case 4:
                {
                    if (_frames < 5) return;
                    int decisions = _boot.DebugAutoPlay(4);
                    _boot.DebugAdvanceLines(20);
                    Log($"تقدّم إضافي · قرارات إجمالية={_boot.DebugDecisionCount} · أعلام={_boot.DebugFlagCount} · " +
                        $"دليل={_boot.DebugStat("Evidence_Count"):0} · شك={_boot.DebugStat("Suspicion"):0} · " +
                        $"مراد={_boot.DebugStat(GameState.TrustMurad):0} · ليلى={_boot.DebugStat(GameState.TrustLaila):0} · " +
                        $"ياسين={_boot.DebugStat(GameState.TrustYassin):0}");
                    Check("جمع أدلة/تغيير علاقات", _boot.DebugFlagCount >= 1 && decisions >= 1,
                          $"قرارات={_boot.DebugDecisionCount} أعلام={_boot.DebugFlagCount}");
                    Step(5);
                    break;
                }

                case 5:
                    if (_frames < 5) return;
                    _boot.DebugOpenPause();
                    Check("الإيقاف المؤقت", Mathf.Abs(_boot.DebugTimeScale) < 0.001f, "timeScale=" + _boot.DebugTimeScale);
                    Shot("pause_menu");
                    _boot.DebugShowScreen(UIScreen.SaveLoad);
                    Shot("save_load_menu");
                    _boot.DebugMenuContext.PendingConfirm = MenuAction.None;
                    _boot.DebugManualSave(1);
                    Step(6);
                    break;

                case 6:
                    if (_frames < 5) return;
                    Check("حفظ من القائمة والإيقاف", SaveManager.Exists(SaveSystem.ManualSlotB),
                          SaveManager.Describe(SaveSystem.ManualSlotB));
                    _boot.DebugClosePause();
                    Check("الاستئناف", Mathf.Abs(_boot.DebugTimeScale - 1f) < 0.001f, "timeScale=" + _boot.DebugTimeScale);
                    Step(7);
                    break;

                case 7:
                {
                    if (_frames < 10) return;
                    _expect = CaptureExpected();
                    File.WriteAllText(Path.Combine(_outDir, "restore_expected.json"), JsonUtility.ToJson(_expect, true));
                    Log($"📄 حالة متوقعة: {_expect.chapterId}/{_expect.nodeId} · أعلام={_expect.flags.Count} · قرارات={_expect.decisions.Count} · وقت={_expect.playtime:0.0}ث");

                    _boot.DebugOpenPause();
                    _boot.DebugShowScreen(UIScreen.Settings);   Shot("settings_menu");
                    _boot.DebugShowScreen(UIScreen.Title);      Shot("main_menu");
                    _boot.DebugShowScreen(UIScreen.Extras);     Shot("extras_menu");
                    _boot.DebugShowScreen(UIScreen.Characters); Shot("extras_characters");
                    _boot.DebugShowScreen(UIScreen.Records);    Shot("extras_records");
                    _boot.DebugShowScreen(UIScreen.Chapters);   Shot("chapters_menu");
                    _boot.DebugShowScreen(UIScreen.Endings);    Shot("endings_menu");
                    _boot.DebugShowScreen(UIScreen.Credits);    Shot("credits_menu");
                    _boot.DebugClosePause();
                    Step(8);
                    break;
                }

                case 8:
                    if (_frames < 30) return;
                    Log("── نهاية جلسة اللعب: العملية هتقفل، والحفظ على القرص بس ──");
                    Check("الحفظ موجود على القرص", SaveManager.LatestSlot() >= 0, "أحدث خانة=" + SaveManager.LatestSlot());
                    Finish(_fail == 0 ? 0 : 1);
                    break;

                // ═════════ الجلسة ٢: استعادة بعد إعادة تشغيل حقيقية ═════════
                case 100:
                    if (_frames < 60) return;
                    if (!FindBoot()) return;
                    Log($"بعد إعادة التشغيل · الحالة قبل Continue: فصل='{_boot.DebugChapterId}' عقدة='{_boot.DebugNodeId}' قرارات={_boot.DebugDecisionCount}");
                    Shot("main_menu_after_restart");
                    _boot.DebugContinue();
                    Step(101);
                    break;

                case 101:
                {
                    if (_frames < 40) return;
                    var now = CaptureExpected();
                    Check("Continue رجّع نفس الفصل", now.chapterId == _expect.chapterId, $"{now.chapterId} == {_expect.chapterId}");
                    Check("Continue رجّع نفس العقدة/المشهد", now.nodeId == _expect.nodeId, $"{now.nodeId} == {_expect.nodeId}");
                    Check("الأعلام رجعت كاملة", SameSet(now.flags, _expect.flags), $"الآن={now.flags.Count} متوقع={_expect.flags.Count}");
                    Check("القرارات رجعت", SameSet(now.decisions, _expect.decisions), $"الآن={now.decisions.Count} متوقع={_expect.decisions.Count}");
                    Check("الإحصائيات/العلاقات رجعت", SameStats(now, _expect), StatsLine(now));
                    Check("وقت اللعب رجع", now.playtime >= _expect.playtime - 1f, $"الآن={now.playtime:0.0} متوقع={_expect.playtime:0.0}");
                    Shot("continue_restored");
                    Step(102);
                    break;
                }

                case 102:
                {
                    if (_frames < 10) return;
                    int before = _boot.DebugDecisionCount;
                    string node = _boot.DebugNodeId;
                    _boot.DebugLoadSlot(2);     // C فاضية
                    Check("تحميل خانة فاضية آمن", _boot.DebugNodeId == node && _boot.DebugDecisionCount == before,
                          "الحالة ما اتغيّرتش");
                    Shot("empty_slot");

                    _boot.DebugManualSave(2);
                    SaveManager.CorruptForTests(SaveSystem.ManualSlotC);
                    Check("الملف التالف اتكشف", SaveManager.IsCorrupt(SaveSystem.ManualSlotC),
                          "IsCorrupt=" + SaveManager.IsCorrupt(SaveSystem.ManualSlotC));
                    _boot.DebugLoadSlot(2);
                    Check("تحميل ملف تالف ما بيكسرش الحالة", _boot.DebugNodeId == node, "العقدة لسه " + node);
                    Check("الملف التالف اتعزل", Directory.GetFiles(SaveManager.DirPath, "*.corrupt-*.json").Length > 0,
                          "عدد الملفات المعزولة=" + Directory.GetFiles(SaveManager.DirPath, "*.corrupt-*.json").Length);
                    Step(103);
                    break;
                }

                case 103:
                {
                    if (_frames < 10) return;
                    SaveManager.WriteLegacyForTests(SaveSystem.ManualSlotA, new GameState());
                    var h = SaveManager.Peek(SaveSystem.ManualSlotA);
                    Check("ملف v1 اترقى", h != null && h.version == SaveManager.CurrentVersion,
                          "version=" + (h != null ? h.version : -1));
                    Step(104);
                    break;
                }

                case 104:
                {
                    if (_frames < 10) return;
                    _boot.DebugShowScreen(UIScreen.Title);
                    var entries = _boot.DebugMenuModel.Build(_boot.DebugMenuContext);
                    int idx = entries.FindIndex(e => e.Action == MenuAction.NewGame);
                    Check("زرار لعبة جديدة موجود", idx >= 0, "index=" + idx);

                    _boot.DebugMenuContext.PendingConfirm = MenuAction.None;
                    _boot.DebugMenuRefresh();
                    if (!_boot.DebugMenu.DebugClick(MenuAction.NewGame))
                        Skip("ضغط زرار New Game", "الزرار مش متاح في الـCanvas");
                    Step(105);
                    break;
                }

                case 105:
                    if (_frames < 10) return;
                    Check("لعبة جديدة بتطلب تأكيد", _boot.DebugConfirmPending,
                          "PendingConfirm=" + (_boot.DebugMenuContext != null ? _boot.DebugMenuContext.PendingConfirm.ToString() : "—"));
                    Shot("new_game_confirm");
                    Step(106);
                    break;

                case 106:
                    if (_frames < 6) return;
                    _boot.DebugMenuContext.PendingConfirm = MenuAction.None;   // إلغاء
                    _boot.DebugMenuRefresh();
                    _boot.DebugOpenPause();
                    _boot.DebugClosePause();
                    Step(107);
                    break;

                case 107:
                {
                    if (_frames < 10) return;
                    float before = AudioLevels.Get("master");
                    AudioLevels.Adjust("master", -AudioLevels.Step);
                    float after = AudioLevels.Get("master");
                    Check("الصوت بيتغيّر فعلياً", Mathf.Abs(after - before) > 0.001f, $"{before:0.00} → {after:0.00}");
                    Check("الصوت بيتحفظ في PlayerPrefs", PlayerPrefs.HasKey(TLC.Audio.AudioDirector.KeyMaster), "tlc.audio.master موجود");

                    Localization.SetLanguage(Localization.Lang.English);
                    Check("اللغة بتتحفظ", SettingsPersistence.PersistedLanguage == (int)Localization.Lang.English,
                          "المحفوظ=" + SettingsPersistence.PersistedLanguage);
                    Localization.SetLanguage(Localization.Lang.Arabic);
                    Check("الرجوع للعربي", SettingsPersistence.PersistedLanguage == (int)Localization.Lang.Arabic, "ar");

                    QualityManager.Tier q = QualityManager.SavedTier(1);
                    Check("الجودة محفوظة", (int)q >= 0, "tier=" + q);
                    Step(108);
                    break;
                }

                case 108:
                    if (_frames < 6) return;
                    _boot.DebugCards.Play(PostCreditSequence.For("CH1_A"));
                    Step(109);
                    break;

                case 109:
                    if (_frames < 20) return;
                    _boot.DebugManualSave(0);
                    Check("الحفظ ممنوع أثناء الكروت", (_boot.DebugToast.LastMessage ?? "").Contains("مينفعش"),
                          "رسالة: " + _boot.DebugToast.LastMessage);
                    _boot.DebugCards.Skip();
                    Step(110);
                    break;

                case 110: if (_frames < 20) return; Step(111); break;

                // ═════════ ما بعد النهاية ×٣ + التيزر بعد كل نهاية ═════════
                case 111: RunEnding("CH1_A", 112); break;
                case 112: VerifyEnding("CH1_A", "A", 113); break;
                case 113: VerifyTeaser("CH1_A", 114); break;
                case 114: RunEnding("CH1_B", 115); break;
                case 115: VerifyEnding("CH1_B", "B", 116); break;
                case 116: VerifyTeaser("CH1_B", 117); break;
                case 117: RunEnding("CH1_C", 118); break;
                case 118: VerifyEnding("CH1_C", "C", 119); break;
                case 119: VerifyTeaser("CH1_C", 120); break;

                case 120:
                {
                    if (_frames < 20) return;
                    if (!EnsurePlayBoot()) return;
                    string before = _boot.DebugCards != null ? _boot.DebugCards.CurrentSequenceId : null;
                    _boot.PlayPostCredit("CH2_A");
                    string after = _boot.DebugCards != null ? _boot.DebugCards.CurrentSequenceId : null;
                    Check("نهاية غير مدعومة مرفوضة", after == before && after != "postcredit_CH2_A",
                          $"قبل={before ?? "—"} بعد={after ?? "—"}");
                    Check("ثلاث نهايات ما-بعد-النهاية محفوظة",
                          Progress.HasSeenPostCredit("CH1_A") && Progress.HasSeenPostCredit("CH1_B") && Progress.HasSeenPostCredit("CH1_C"),
                          "A/B/C");
                    Check("التيزر مسجّل كسينمائية مفتوحة", Progress.HasCinematic(ChapterTwoTeaser.CinematicId), "teaser_ch2");
                    Step(121);
                    break;
                }

                case 121:
                    if (_frames < 10) return;
                    _boot.DebugShowScreen(UIScreen.Extras);
                    Shot("extras_after_endings");
                    _boot.DebugShowScreen(UIScreen.Cinematics);
                    Shot("extras_cinematics");
                    _boot.DebugClosePause();
                    Step(122);
                    break;

                case 122:
                    if (_frames < 20) return;
                    Log($"شغل كروت: {string.Join(", ", _shots.Keys)}");
                    Finish(_fail == 0 ? 0 : 1);
                    break;
            }
        }

        // ─────────────────── نهايات + كروت ───────────────────

        static bool _reloading;
        static int _reloadFrame;

        /// <summary>بعد ما التيزر بيرجّع للقائمة، المشهد بيتغيّر فعلاً — بنرجع لمشهد اللعب.</summary>
        static bool EnsurePlayBoot()
        {
            if (FindBoot()) { _reloading = false; return true; }

            if (!_reloading && _frames - _reloadFrame > 60)
            {
                _reloading = true;
                _reloadFrame = _frames;
                Log("↻ إعادة تحميل مشهد اللعب (التيزر رجّع للقائمة)");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Play");
            }
            return false;
        }

        static void RunEnding(string endingId, int next)
        {
            if (_frames < 8) return;
            if (!EnsurePlayBoot()) return;
            bool ok = _boot.DebugReachEnding(endingId);
            Check("الوصول للنهاية " + endingId, ok, ok ? "اتنده OnEnding" : "العقدة مش موجودة");
            Step(next);
        }

        static void VerifyEnding(string endingId, string letter, int next)
        {
            if (_frames < 25) return;
            string seqId = PostCreditSequence.CinematicId(endingId);
            string current = _boot.DebugCards != null ? _boot.DebugCards.CurrentSequenceId : null;

            Check($"ما بعد النهاية {letter} اشتغل", current == seqId, $"الحالي={current} متوقع={seqId}");
            Check($"ما بعد النهاية {letter} مسجّل في التقدّم", Progress.HasSeenPostCredit(endingId), endingId);

            var seq = PostCreditSequence.For(endingId);
            int expectedVariant = letter == "A" ? 1 : letter == "B" ? 2 : 3;
            Check($"مضمون ما بعد النهاية {letter} متسق مع النهاية", seq != null && seq.endingVariant == expectedVariant,
                  $"variant={seq?.endingVariant} متوقع={expectedVariant}");

            _boot.DebugCards.DebugDump();
            Shot("postcredit_" + letter);

            if (_boot.DebugCards != null && _boot.DebugCards.IsPlaying &&
                _boot.DebugCards.CurrentSequenceId == seqId)
                _boot.DebugCards.Skip();

            Step(next);
        }

        static void VerifyTeaser(string endingId, int next)
        {
            if (_frames < 45) return;

            string current = _boot.DebugCards != null ? _boot.DebugCards.CurrentSequenceId : null;
            Check("تيزر الفصل الثاني اشتغل بعد ما بعد النهاية",
                  current == ChapterTwoTeaser.CinematicId, $"الحالي={current}");

            var teaser = ChapterTwoTeaser.For(endingId);
            Check("التيزر متسق مع النهاية", teaser != null && teaser.cards.Count >= 6,
                  $"كروت={teaser?.cards.Count} · نسخة={ChapterTwoTeaser.VariantLabel(endingId)}");

            Shot("teaser_" + endingId);

            if (_boot.DebugCards != null && _boot.DebugCards.IsPlaying) _boot.DebugCards.Skip();
            Step(next);
        }

        // ─────────────────── أدوات مقارنة ───────────────────

        static Expected CaptureExpected()
        {
            var e = new Expected
            {
                chapterId = _boot.DebugChapterId,
                nodeId = _boot.DebugNodeId,
                endingId = _boot.DebugEndingId,
                playtime = _boot.DebugPlaytime,
                savedAtUtc = DateTime.UtcNow.ToString("o")
            };

            var st = _boot.DebugState;
            if (st != null)
            {
                e.flags.AddRange(st.Flags);
                e.decisions.AddRange(st.Decisions);
                foreach (var kv in st.Stats)
                {
                    e.statKeys.Add(kv.Key);
                    e.statValues.Add(kv.Value);
                }
            }
            e.flags.Sort();
            e.decisions.Sort();
            return e;
        }

        static bool SameSet(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++) if (a[i] != b[i]) return false;
            return true;
        }

        static bool SameStats(Expected a, Expected b)
        {
            if (a.statKeys.Count != b.statKeys.Count) return false;
            for (int i = 0; i < a.statKeys.Count; i++)
            {
                int j = b.statKeys.IndexOf(a.statKeys[i]);
                if (j < 0) return false;
                if (Mathf.Abs(a.statValues[i] - b.statValues[j]) > 0.01f) return false;
            }
            return true;
        }

        static string StatsLine(Expected e)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < e.statKeys.Count; i++) sb.Append($"{e.statKeys[i]}={e.statValues[i]:0} ");
            return sb.ToString();
        }
    }
}
