using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TLC.Narrative;
using TLC.UI;

namespace TLC.EditorTools
{
    /// <summary>
    /// مشغّل الفصل الأول آليًا في Play Mode الحقيقي، بيلعب من أول عقدة لآخر نهاية:
    ///
    ///   • بيدوس «لعبة جديدة» بضغطة حقيقية على الزرار (نفس مسار EventSystem).
    ///   • بيمشي السرد عقدة عقدة عن طريق NarrativeRunner.Advance() (نفس اللي بيحصل لما
    ///     اللاعب يدوس على الشاشة)، وبيختار القرارات عن طريق NarrativeRunner.Choose(i)
    ///     (نفس اللي بيحصل لما يدوس على خيار).
    ///   • بيصوّر لحظة دخول كل مشهد (act) وكل قرار والنهاية — صور من الكاميرا الحقيقية.
    ///   • بيسجّل كل عقدة (id · المشهد · اللقطة · المتحدث) + المتغيرات في آخر الجولة.
    ///
    /// السياسة بتتحدد من سطر الأوامر:
    ///   -choices 0,1,2   ← فهرس الخيار المختار بالترتيب (بيتكرر آخر رقم لو القرارات أكتر)
    ///   -captureDir      ← مجلد الصور
    ///   -tag             ← بادئة أسماء الصور والسجل (a/b/c)
    ///
    /// الاستخدام:
    ///   Unity -executeMethod TLC.EditorTools.ChapterPlaytestCapture.Run -choices 0,0,0,0,0,0,0,0
    /// </summary>
    [InitializeOnLoad]
    public static class ChapterPlaytestCapture
    {
        const string SessionKey = "TLC_ChapterRun";
        const string MenuScene  = "Assets/_Project/Scenes/Main.unity";

        static string _outDir   = "Screenshots/chapter";
        static string _tag      = "run";
        static int[]  _policy   = { 0 };
        static bool   _probe;

        static bool _running, _playSeen;
        static int  _state, _frames;
        static float _clock;

        static MenuSystem _menu;
        static NarrativeRunner _runner;
        static Camera _cam;

        // حالة السرد
        static StoryNode _line;
        static StoryNode _choiceNode;
        static List<ChoiceOption> _choiceOptions;
        static StoryNode _ending;
        static string _act = "-";
        static int _choiceIndex, _shotIndex, _lines, _choices, _steps;
        static string _lastChoiceId;
        static int _sameChoice;
        static readonly StringBuilder _trace = new StringBuilder();
        static readonly List<string> _log = new List<string>();
        static bool _subscribed;
        static float _stepDelay = 0.30f;
        static string _pendingShot;
        static float _pendingShotAt;
        static float _nextStepAt;

        static ChapterPlaytestCapture()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionKey + "Dir", _outDir);
                _tag    = SessionState.GetString(SessionKey + "Tag", _tag);
                _policy = ParsePolicy(SessionState.GetString(SessionKey + "Pol", "0"));
                _probe = SessionState.GetBool(SessionKey + "Probe", false);
                _running = true; _state = 0; _frames = 0; _playSeen = false;
                EditorApplication.update += Tick;
                Debug.Log("[TLC/Play] استأنفت بعد إعادة تحميل الأسمبلي");
            }
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];
                if (args[i] == "-tag" && i + 1 < args.Length) _tag = args[i + 1];
                if (args[i] == "-choices" && i + 1 < args.Length) _policy = ParsePolicy(args[i + 1]);
                if (args[i] == "-probe") _probe = true;
            }
            Directory.CreateDirectory(_outDir);

            SessionState.SetBool(SessionKey, true);
            SessionState.SetString(SessionKey + "Dir", _outDir);
            SessionState.SetString(SessionKey + "Tag", _tag);
            SessionState.SetString(SessionKey + "Pol", string.Join(",", _policy));
            SessionState.SetBool(SessionKey + "Probe", _probe);

            Log($"جولة «{_tag}» · سياسة الاختيارات = {string.Join(",", _policy)} · Unity {Application.unityVersion}");
            Log("الرسّام: " + SystemInfo.graphicsDeviceName);

            // مقاس نافذة Game View: في التشغيل الرسومي (headless GUI) الويندو بيبدأ
            // صغير جدًا (اتشاف 691x79) وده بيخلّي الكانفس يتحسب بمقاس غلط والنص
            // يطلع بره الصندوق. فبنثبّت المقاس 1280x720 قبل الدخول في Play.
            SetGameViewSize(1280, 720);

            EditorSceneManager.OpenScene(MenuScene);
            _running = true; _state = 0; _frames = 0; _playSeen = false;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        /// <summary>تثبيت مقاس Game View (1280x720) قبل التشغيل.</summary>
        static void SetGameViewSize(int width, int height)
        {
            try
            {
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                var sizesType   = Type.GetType("UnityEditor.GameViewSizes,UnityEditor");
                var sizeType    = Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                if (gameViewType == null || sizesType == null || sizeType == null)
                { Log("⚠ مقدرتش أوصل لأنواع GameView — هكمّل بالمقاس الحالي"); return; }

                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);

                var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
                var instance = singletonType.GetProperty("instance").GetValue(null, null);
                var group = sizesType.GetProperty("currentGroup").GetValue(instance, null);
                var groupType = group.GetType();

                var getTotalCount  = groupType.GetMethod("GetTotalCount");
                var getGameViewSize = groupType.GetMethod("GetGameViewSize");
                var addCustomSize  = groupType.GetMethod("AddCustomSize");

                int total = (int)getTotalCount.Invoke(group, null);
                int target = -1;
                for (int i = 0; i < total; i++)
                {
                    var size = getGameViewSize.Invoke(group, new object[] { i });
                    int w = (int)size.GetType().GetProperty("width").GetValue(size);
                    int h = (int)size.GetType().GetProperty("height").GetValue(size);
                    if (w == width && h == height) { target = i; break; }
                }

                if (target < 0)
                {
                    var ctor = sizeType.GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(string) });
                    var fixedSize = ctor.Invoke(new object[] { 1, width, height, $"{width}x{height}" });
                    addCustomSize.Invoke(group, new[] { fixedSize });
                    target = (int)getTotalCount.Invoke(group, null) - 1;
                }

                var select = gameViewType.GetMethod("SizeSelectionCallback",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                select.Invoke(gameView, new object[] { target, null });
                gameView.Repaint();

                Log($"مقاس Game View اتثبّت: {width}x{height} (فهرس {target})");
            }
            catch (Exception e) { Log("⚠ SetGameViewSize: " + e.Message); }
        }

        static int[] ParsePolicy(string s)
        {
            var parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<int>();
            foreach (var p in parts) if (int.TryParse(p, out int v)) list.Add(v);
            if (list.Count == 0) list.Add(0);
            return list.ToArray();
        }

        static int Policy(int choiceIndex) => _policy[Mathf.Min(choiceIndex, _policy.Length - 1)];

        // ─────────────────────────── الحلقة ───────────────────────────

        static void Tick()
        {
            if (!_running) return;

            if (!EditorApplication.isPlaying)
            {
                if (_playSeen) { Log("✗ Play Mode وقف قبل ما الفصل يخلص"); Finish(3); }
                return;
            }

            if (!_playSeen) { _playSeen = true; Log("Play Mode بدأ"); return; }

            _frames++;
            _clock = Time.realtimeSinceStartup;

            switch (_state)
            {
                case 0:
                    _menu = UnityEngine.Object.FindFirstObjectByType<MenuSystem>();
                    if (_menu == null || _frames < 50) return;
                    if (!_menu.IsOpen) _menu.Show(UIScreen.Title);
                    _log.Add("المنيو: " + (_menu.IsOpen ? "مفتوحة ✅" : "مقفولة ❌"));
                    Next();
                    break;

                case 1:
                    if (_frames < 20) return;
                    _log.Add("دوسة «لعبة جديدة»: " + (ClickButton("لعبة جديدة") ? "✅" : "❌"));
                    Next();
                    break;

                case 2:
                    if (_frames < 60) return;
                    _runner = UnityEngine.Object.FindFirstObjectByType<NarrativeRunner>();
                    if (_runner == null) { Log("✗ مفيش NarrativeRunner"); Finish(4); return; }
                    Subscribe();
                    Log("الفصل بدأ · عُقد الملف = " + (_runner.Story != null && _runner.Story.Document != null
                        ? _runner.Story.Document.nodes.Length.ToString() : "?"));
                    Next();
                    break;

                case 3:  // تثبيت مقاس الرسم 1280x720 على الكاميرا نفسها
                    if (_frames < 30) return;
                    SetupRenderTarget();
                    Next();
                    break;

                case 4:  // اللعب الآلي
                    if (_frames == 12) DumpUI();
                    if (_probe && _frames == 40) { DumpUIAll(); Capture("probe_ui"); Finish(0); return; }
                    DriveStory();
                    break;
            }
        }

        static RenderTexture _rt;

        /// <summary>
        /// بنخلي الكاميرا ترسم في RenderTexture بمقاس 1280x720 بدل نافذة الـGame View.
        /// السبب: في التشغيل الرسومي على السيرفر نافذة Game View مقاسها 691x79،
        /// والكانفس Screen Space - Camera بياخد مقاسه من الكاميرا ⇒ الواجهة كانت
        /// بتتحسب على 691x79 والنص بيطلع بره الصندوق. لما الكاميرا ترسم في RT،
        /// الكانفس بياخد 1280x720 الحقيقي.
        /// </summary>
        static void SetupRenderTarget()
        {
            _cam = ResolveCamera();
            if (_cam == null) { Log("✗ مفيش كاميرا لتثبيت المقاس"); return; }

            _rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _rt.Create();
            _cam.targetTexture = _rt;
            Log($"الكاميرا بترسم في RT {_rt.width}x{_rt.height} · canvas هيتحسب على نفس المقاس");
        }

        static void Next() { _state++; _frames = 0; }

        /// <summary>تخطيط كل عناصر الواجهة: المقاس + حدودها على الشاشة.</summary>
        static void DumpUIAll()
        {
            _cam = ResolveCamera();
            Log($"── تخطيط الواجهة الكامل · شاشة {Screen.width}x{Screen.height} · كاميرا RT={( _cam && _cam.targetTexture ? _cam.targetTexture.width + "x" + _cam.targetTexture.height : "لا")} ──");
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!c.name.StartsWith("UI_")) continue;
                var crt = c.GetComponent<RectTransform>();
                Log($"◼ {c.name}: rect={crt.rect.size} scale={crt.localScale.x:0.000} order={c.sortingOrder}");
                foreach (var rt in c.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt == crt) continue;
                    var tmp = rt.GetComponent<TMPro.TextMeshProUGUI>();
                    var corners = new Vector3[4];
                    rt.GetWorldCorners(corners);
                    float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                    foreach (var p in corners)
                    {
                        var sp = _cam != null ? _cam.WorldToScreenPoint(p) : p;
                        minX = Mathf.Min(minX, sp.x); maxX = Mathf.Max(maxX, sp.x);
                        minY = Mathf.Min(minY, sp.y); maxY = Mathf.Max(maxY, sp.y);
                    }
                    string extra = tmp != null
                        ? $" · font={tmp.fontSize} · wrap={tmp.textWrappingMode} · align={tmp.alignment} · txt='{Trim(tmp.text)}'"
                        : "";
                    Log($"   {(rt.gameObject.activeSelf ? "·" : "×")} {rt.name}: size={rt.rect.size} " +
                        $"screen=[{minX:0},{minY:0}→{maxX:0},{maxY:0}]{extra}");
                }
            }
        }

        /// <summary>تشخيص الواجهة: كل كانفس ومقاسات صندوق الحوار والنص.</summary>
        static void DumpUI()
        {
            Log($"── تشخيص الواجهة · الشاشة {Screen.width}x{Screen.height} ──");
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                var sc = c.GetComponent<CanvasScaler>();
                Log($"  كانفس '{c.name}' · mode={c.renderMode} · cam={(c.worldCamera ? c.worldCamera.name : "NULL")} · " +
                    $"order={c.sortingOrder} · ref={(sc ? sc.referenceResolution.ToString() : "-")} · " +
                    $"match={(sc ? sc.matchWidthOrHeight.ToString("0.00") : "-")} · rect={c.GetComponent<RectTransform>().rect.size}");
            }

            var ui = UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
            if (ui == null) { Log("  مفيش DialogueUI"); return; }

            var tr = ui.transform;
            var all = tr.GetComponentsInChildren<RectTransform>(true);
            Log($"  عناصر RectTransform تحت DialogueUI: {all.Length}");
            foreach (var rt in all)
            {
                var tmp = rt.GetComponent<TMPro.TextMeshProUGUI>();
                string label = tmp != null ? $"TXT[{Trim(tmp.text)}]" : "";
                if (rt.name != "Dialogue" && rt.name != "Body" && rt.name != "Speaker" &&
                    rt.name != "Choices" && rt.name != "SafeArea" && rt.name != "Hint" && tmp == null) continue;

                Vector3[] corners = new Vector3[4];
                rt.GetWorldCorners(corners);
                Log($"   · {rt.name}: size={rt.rect.size} · pos={rt.position} · " +
                    $"screen=[{corners[0].x:0},{corners[0].y:0} → {corners[2].x:0},{corners[2].y:0}] " +
                    (tmp != null ? $"· font={tmp.fontSize} · wrap={tmp.textWrappingMode} · align={tmp.alignment} " : "") + label);
            }
        }

        static void Subscribe()
        {
            if (_subscribed || _runner == null) return;
            _subscribed = true;
            _runner.LineShown += n => { _line = n; _lines++; };
            _runner.ChoicesShown += (n, opts) => { _choiceNode = n; _choiceOptions = opts; };
            _runner.EndingReached += n => { _ending = n; };
        }

        static void DriveStory()
        {
            if (_clock < _nextStepAt) return;
            _nextStepAt = _clock + _stepDelay;

            if (!string.IsNullOrEmpty(_pendingShot) && _clock >= _pendingShotAt)
            {
                Capture(_pendingShot);
                _pendingShot = null;
            }

            if (_ending != null) { FinishRun(0); return; }

            if (++_steps > 1500) { Log("✗ تخطّينا 1500 خطوة — الفصل مش بيكمل (حلقة؟)"); FinishRun(6); return; }

            if (_choiceOptions != null && _choiceOptions.Count > 0)
            {
                if (_choiceNode != null && _choiceNode.id == _lastChoiceId) _sameChoice++;
                else { _sameChoice = 0; _lastChoiceId = _choiceNode != null ? _choiceNode.id : null; }
                if (_sameChoice > 12) { Log($"✗ قرار '{_lastChoiceId}' بيعيد نفسه {_sameChoice} مرة — حلقة"); FinishRun(7); return; }

                var node = _choiceNode;
                Capture($"{_tag}_choice{_choiceIndex + 1:00}_{Short(node.id)}");
                int pick = Mathf.Clamp(Policy(_choiceIndex), 0, _choiceOptions.Count - 1);
                var opt = _choiceOptions[pick];

                Log($"قرار {node.id} → «{opt.text}» ({opt.decisionId})");
                _trace.AppendLine($"C|{node.id}|{opt.decisionId}|{opt.text}");

                _choiceOptions = null; _choiceNode = null;
                _choiceIndex++; _choices++;
                _stepDelay = 0.30f;
                _runner.Choose(pick);
                return;
            }

            if (_runner.IsWaitingForInput && _line != null)
            {
                // مشهد جديد؟ صوّر.
                if (!string.IsNullOrEmpty(_line.act) && _line.act != _act)
                {
                    _act = _line.act;
                    // بنصوّر بعد ما تأثير الكتابة يخلص مش في أول فريم
                    _pendingShot = $"{_tag}_scene_{_act}";
                    _pendingShotAt = _clock + 1.2f;
                    Log($"— مشهد {_act} (عقدة {_line.id})");
                }

                _trace.AppendLine($"L|{_line.id}|{_line.act}|{_line.camera}|{_line.speaker}|{Trim(_line.text)}");
                _line = null;
                _stepDelay = 0.28f;
                _runner.Advance();
                return;
            }

            // عقدة محجوبة (حدث صوت/سينمائية/تفاعل) — بنفكّها زي ما اللاعب بيعمل
            if (_runner.IsBlocked)
            {
                var pending = _runner.PendingInteractionId;
                if (!string.IsNullOrEmpty(pending))
                {
                    Log($"تفاعل مطلوب: '{pending}' — بنفّذه");
                    _runner.NotifyInteraction(pending);
                }
                else
                {
                    _runner.ResumeAfterEvent();
                }
            }
        }

        // ─────────────────────────── النهاية ───────────────────────────

        static void FinishRun(int code)
        {
            Capture($"{_tag}_ending_{Short(_ending != null ? _ending.endingId : "none")}");

            Log($"★ نهاية: {_ending.endingId} — {_ending.endingTitle}");
            Log($"  سطور اتقدمت: {_lines} · قرارات: {_choices}");

            var st = _runner.State;
            string[] keys =
            {
                "Trust_Murad", "Trust_Laila", "Trust_Yassin", "Evidence_Count", "Suspicion"
            };
            foreach (var k in keys) Log($"  {k} = {st.Get(k):0.#}");

            string[] flags =
            {
                "KNOWS_SELIM", "ENVELOPE_KEPT", "FOUND_ARCHIVE_FILE", "FOUND_CONTRACT_NUMBER",
                "REVEALED_LAILA", "TOLD_YASSIN_ABOUT_LAILA", "MURAD_NAMED_LAILA",
                "LAILA_SUSPECTS_YASSIN", "YASSIN_REOPENED_CASE", "ALLY_MURAD", "ALLY_LAILA", "ALLY_YASSIN"
            };
            var on = new List<string>();
            foreach (var f in flags) if (st.HasFlag(f)) on.Add(f);
            Log("  أعلام مفعّلة: " + (on.Count > 0 ? string.Join(" · ", on) : "(مفيش)"));
            Log("  قرارات محفوظة في ChoiceHistory: " + st.Decisions.Count);

            File.WriteAllText(Path.Combine(_outDir, $"{_tag}_trace.txt"), _trace.ToString());
            Finish(code);
        }

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;
            SessionState.SetBool(SessionKey, false);

            var sb = new StringBuilder();
            foreach (var l in _log) sb.AppendLine(l);
            Directory.CreateDirectory(_outDir);
            File.WriteAllText(Path.Combine(_outDir, $"{_tag}_playtest.log"), sb.ToString());

            Debug.Log(sb.ToString());
            Debug.Log($"[TLC/Play] خلصت — كود {code}");

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }

        static string Trim(string s) => string.IsNullOrEmpty(s) ? "" :
            (s.Length > 60 ? s.Substring(0, 60).Replace("\n", " ") + "…" : s.Replace("\n", " "));

        static string Short(string id) => string.IsNullOrEmpty(id) ? "-" :
            id.Replace("c1_", "").Replace("CH1_", "");

        // ─────────────────────────── أدوات ───────────────────────────

        static bool ClickButton(string label)
        {
            foreach (var b in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                var t = b.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (t == null) continue;
                // نص الزرار بقى مشكّل (Presentation Forms + ترتيب بصري)، فبنقارن
                // بالشكل النهائي كمان مش بالنص الخام بس.
                string shaped = TLC.Settings.ArabicShaper.Fix(label);
                if (!t.text.Contains(label) && !t.text.Contains(shaped)) continue;
                ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current)
                { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
                return true;
            }
            return false;
        }

        static Camera ResolveCamera()
        {
            var cam = Camera.main;
            if (cam != null && cam.isActiveAndEnabled) return cam;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c != null && c.isActiveAndEnabled) return c;
            return null;
        }

        static void Capture(string name)
        {
            _cam = ResolveCamera();
            if (_cam == null) { Log("✗ مفيش كاميرا"); return; }

            UIFactory.BindAllCanvasesToCamera(_cam);
            Canvas.ForceUpdateCanvases();

            const int W = 1280, H = 720;
            var rt = _rt != null ? _rt : new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prevT = _cam.targetTexture;
            var prevA = RenderTexture.active;
            bool temp = _rt == null;
            try
            {
                if (temp) _cam.targetTexture = rt;
                _cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();

                string file = name + ".png";
                File.WriteAllBytes(Path.Combine(_outDir, file), tex.EncodeToPNG());
                Log($"📷 {file} · تباين={Contrast(tex):0.000}");
                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e) { Log("✗ تصوير فشل: " + e.Message); }
            finally
            {
                _cam.targetTexture = temp ? prevT : _rt;
                RenderTexture.active = prevA;
                if (temp) { rt.Release(); UnityEngine.Object.DestroyImmediate(rt); }
            }
        }

        static float Contrast(Texture2D tex)
        {
            float min = 1f, max = 0f;
            for (int y = tex.height / 6; y < tex.height * 5 / 6; y += 17)
                for (int x = tex.width / 12; x < tex.width * 11 / 12; x += 23)
                {
                    var c = tex.GetPixel(x, y);
                    float l = (c.r + c.g + c.b) / 3f;
                    if (l < min) min = l;
                    if (l > max) max = l;
                }
            return max - min;
        }

        static void Log(string msg) { _log.Add(msg); }
    }
}
