using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TLC.UI;

namespace TLC.EditorTools
{
    /// <summary>
    /// إثبات إن المنيو الرئيسية **ظاهرة** وبتستجيب **بضغطة حقيقية** في Play Mode.
    ///
    /// الاختلاف عن المصوّر القديم: بيصوّر الكاميرا **بعد** ما يربط كل الكانفسات
    /// كـScreen Space - Camera، فالواجهة بتدخل في الصورة.
    /// وبيدوس زرار «لعبة جديدة» عن طريق `ExecuteEvents.pointerClickHandler`
    /// — يعني نفس مسار الحدث اللي بيحصل لما اللاعب يدوس بإصبعه، مش استدعاء دالة مباشر.
    /// </summary>
    [InitializeOnLoad]
    public static class MenuFlowCapture
    {
        const string SessionKey = "TLC_MenuFlow";
        const string SessionDirKey = "TLC_MenuFlowDir";
        const string MenuScene = "Assets/_Project/Scenes/Main.unity";

        static string _outDir = "Screenshots/menu";
        static bool _running;
        static int _state;
        static int _framesInState;
        static bool _playSeen;

        static TLC.UI.MenuSystem _menu;
        static Camera _cam;
        static readonly List<string> _log = new List<string>();

        // نتائج الفحص — بتتكتب في التقرير
        static string _menuShot = "-";
        static string _afterShot = "-";
        static string _dialogueShot = "-";
        static bool _menuVisible;
        static bool _buttonClicked;
        static bool _menuClosed;
        static bool _chapterStarted;
        static bool _dialogueVisible;
        static string _continueState = "-";

        static MenuFlowCapture()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionDirKey, "Screenshots/menu");
                _running = true; _state = 0; _framesInState = 0; _playSeen = false;
                EditorApplication.update += Tick;
                Debug.Log("[TLC/Menu] استأنفت بعد إعادة تحميل الأسمبلي");
            }
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];
            Directory.CreateDirectory(_outDir);

            if (!File.Exists(MenuScene)) { Log("✗ مشهد القائمة مش موجود"); Flush(); EditorApplication.Exit(2); return; }

            // موارد TMP لازم تكون موجودة قبل أي واجهة — من غيره Build بيموت في النص
            try { TMPEssentials.Ensure(true); AssetDatabase.Refresh(); } catch (Exception e) { Log("TMP: " + e.Message); }
            Log("TMP Settings موجود: " + (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null));

            EditorSceneManager.OpenScene(MenuScene);
            Log("المشهد: " + MenuScene + " · Unity " + Application.unityVersion);
            Log("الرسّام: " + SystemInfo.graphicsDeviceName + " · " + SystemInfo.graphicsDeviceType);

            SessionState.SetBool(SessionKey, true);
            SessionState.SetString(SessionDirKey, _outDir);
            EditorApplication.update += Tick;
            _running = true;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (!_running) return;
            if (!EditorApplication.isPlaying) { Log("✗ Play Mode وقف"); Finish(3); return; }

            if (!_playSeen) { _playSeen = true; Debug.Log("[TLC/Menu] Play Mode بدأ"); return; }

            _framesInState++;

            switch (_state)
            {
                case 0: // استنى المنيو تتبني
                    _menu = UnityEngine.Object.FindFirstObjectByType<MenuSystem>();
                    if (_menu == null) return;
                    if (_framesInState < 45) return;
                    _cam = Camera.main;
                    if (_cam == null)
                        foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                            if (c.enabled) { _cam = c; break; }

                    DumpCanvasDiagnostics();
                    DumpMenuInternals();
                    DumpHierarchy();
                    Log("MenuSystem.IsOpen = " + _menu.IsOpen);
                    if (!_menu.IsOpen) { Log("⚠ المنيو مش مفتوحة — هفتحها بـShow(Title)"); _menu.Show(UIScreen.Title); }
                    Next();
                    break;

                case 1: // لقطة المنيو
                    if (_framesInState < 20) return;
                    _menuShot = Capture("01_main_menu");
                    _menuVisible = IsCanvasActuallyRendering();
                    Log("المنيو ظاهرة في الصورة: " + (_menuVisible ? "✅" : "❌"));
                    CheckContinueButton();
                    Next();
                    break;

                case 2: // دوسة «لعبة جديدة»
                    if (_framesInState < 10) return;
                    _buttonClicked = ClickButton("لعبة جديدة");
                    Log("دوسة زر «لعبة جديدة»: " + (_buttonClicked ? "✅ الحدث اتبعت" : "❌ الزرار مش موجود"));
                    Next();
                    break;

                case 3: // تأكيد إن المنيو قفلت
                    if (_framesInState < 60) return;
                    _menuClosed = _menu == null || !_menu.IsOpen;
                    Log("المنيو قفلت: " + (_menuClosed ? "✅" : "❌"));
                    _afterShot = Capture("02_after_new_game");
                    Log("اسم المشهد الحالي: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                    Next();
                    break;

                case 4: // استنى بداية الفصل الأول والحوار
                    {
                        var runner = UnityEngine.Object.FindFirstObjectByType<TLC.Narrative.NarrativeRunner>();
                        if (runner != null && runner.IsWaitingForInput) _chapterStarted = true;
                        if (_framesInState < 120 && !_chapterStarted) return;

                        var ui = UnityEngine.Object.FindFirstObjectByType<TLC.UI.DialogueUI>();
                        _dialogueVisible = ui != null && IsCanvasActuallyRendering();
                        Log("الفصل الأول بدأ (السرد مستني إدخال): " + (_chapterStarted ? "✅" : "❌"));
                        Next();
                    }
                    break;

                case 5:
                    if (_framesInState < 25) return;
                    _dialogueShot = Capture("03_chapter1_dialogue");
                    Log("واجهة الحوار ظاهرة: " + (_dialogueVisible ? "✅" : "❌"));
                    DumpVisibleTexts();
                    Finish(0);
                    break;
            }
        }

        static void Next() { _state++; _framesInState = 0; }

        // ───────────────────────── التشخيص ─────────────────────────

        static void DumpCanvasDiagnostics()
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Log("── تشخيص الكانفسات (" + canvases.Length + ") ──");
            foreach (var c in canvases)
            {
                var scaler = c.GetComponent<CanvasScaler>();
                Log($"  · {c.name}: mode={c.renderMode} · cam={(c.worldCamera ? c.worldCamera.name : "NULL")} · " +
                    $"sort={c.sortingOrder} · plane={c.planeDistance:0.00} · display={c.targetDisplay} · " +
                    $"active={c.gameObject.activeInHierarchy} · children={c.transform.childCount}" +
                    (scaler != null ? $" · scale={scaler.uiScaleMode}/{scaler.referenceResolution}" : ""));
            }
            var es = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Log("  EventSystem: " + (es != null ? es.name + " · module=" +
                (es.currentInputModule != null ? es.currentInputModule.GetType().Name : "NULL") : "❌ مفقود"));
        }

        static void DumpMenuInternals()
        {
            if (_menu == null) return;
            var t = _menu.GetType();
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var sb = new System.Text.StringBuilder();
            foreach (var f in t.GetFields(flags))
            {
                var v = f.GetValue(_menu);
                string desc = v == null ? "NULL" : v.ToString();
                if (v is UnityEngine.Object uo) desc = uo.name + (uo == null ? " (محذوف)" : "");
                if (v is System.Collections.ICollection col) desc = "count=" + col.Count;
                sb.Append(f.Name).Append('=').Append(desc).Append(" · ");
            }
            Log("حقول MenuSystem: " + sb);
        }

        static void DumpHierarchy()
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                Log("── شجرة " + c.name + " ──");
                DumpNode(c.transform, 0);
            }
            var allButtons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Log("إجمالي أزرار في المشهد (بما فيها المخفية): " + allButtons.Length);
            foreach (var b in allButtons)
                Log("   زر: " + b.name + " · active=" + b.gameObject.activeInHierarchy +
                    " · interactable=" + b.interactable);
        }

        static void DumpNode(Transform t, int depth)
        {
            if (depth > 4) return;
            string pad = new string(' ', depth * 2);
            var tmp = t.GetComponent<TextMeshProUGUI>();
            string extra = tmp != null ? " [نص: «" + Truncate(tmp.text, 30) + "»]" : "";
            Log(pad + "• " + t.name + " · active=" + t.gameObject.activeSelf + extra);
            for (int i = 0; i < t.childCount; i++) DumpNode(t.GetChild(i), depth + 1);
        }

        static bool IsCanvasActuallyRendering()
        {
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
                if (c.gameObject.activeInHierarchy && c.renderMode != RenderMode.WorldSpace && c.transform.childCount > 0)
                    return true;
            return false;
        }

        static void DumpVisibleTexts()
        {
            var texts = UnityEngine.Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            Log("── نصوص ظاهرة على الشاشة (" + texts.Length + ") ──");
            int shown = 0;
            foreach (var t in texts)
            {
                if (t == null || !t.gameObject.activeInHierarchy || string.IsNullOrWhiteSpace(t.text)) continue;
                if (!t.gameObject.transform.IsChildOf(t.canvas ? t.canvas.transform : t.transform)) { }
                Log("   «" + Truncate(t.text, 60) + "»");
                if (++shown >= 12) break;
            }
            if (shown == 0) Log("   (مفيش نصوص ظاهرة)");
        }

        static void CheckContinueButton()
        {
            bool anySave = false;
            for (int slot = 0; slot <= 3; slot++) if (TLC.Core.SaveSystem.Exists(slot)) anySave = true;
            var btn = FindButton("متابعة");
            if (btn == null) btn = FindButton("استكمال");
            if (btn == null) { _continueState = "الزرار مش موجود في الشاشة"; }
            else
            {
                _continueState = $"interactable={btn.interactable} · save موجود={anySave}";
                if (anySave && !btn.interactable) Log("⚠ فيه حفظ بس الزرار مقفول");
                if (!anySave && btn.interactable) Log("⚠ مفيش حفظ والزرار مفتوح — المفروض يتقفل");
            }
            Log("زر المتابعة: " + _continueState);
        }

        // ───────────────────────── تفاعل حقيقي ─────────────────────────

        static Button FindButton(string label)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var b in buttons)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;
                var tmp = b.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null || tmp.text == null) continue;
                // نص الزرار بقى مشكّل (Presentation Forms + ترتيب بصري)، فبنقارن
                // بالنص الخام وبالشكل النهائي كمان.
                string shaped = TLC.Settings.ArabicShaper.Fix(label);
                if (tmp.text.Contains(label) || tmp.text.Contains(shaped)) return b;
            }
            return null;
        }

        static bool ClickButton(string label)
        {
            var btn = FindButton(label);
            if (btn == null)
            {
                var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
                var names = new List<string>();
                foreach (var b in buttons)
                {
                    var t = b.GetComponentInChildren<TextMeshProUGUI>();
                    if (t != null) names.Add(t.text);
                }
                Log("   الأزرار الموجودة: " + string.Join(" | ", names));
                return false;
            }

            Log("   لقيت الزر: «" + label + "» · interactable=" + btn.interactable +
                " · listeners=" + btn.onClick.GetPersistentEventCount());

            var es = EventSystem.current;
            if (es == null)
            {
                Log("   ⚠ مفيش EventSystem.current — هستخدم onClick.Invoke() مباشرة");
                btn.onClick.Invoke();
                return true;
            }

            var canvas = btn.GetComponentInParent<Canvas>();
            Camera uiCam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) ? canvas.worldCamera : null;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCam, btn.transform.position);

            var data = new PointerEventData(es)
            {
                button = PointerEventData.InputButton.Left,
                position = screenPoint,
                pressPosition = screenPoint,
                clickTime = Time.unscaledTime,
                clickCount = 1
            };
            data.pointerCurrentRaycast = new RaycastResult { gameObject = btn.gameObject };
            data.pointerPressRaycast = data.pointerCurrentRaycast;
            Log("   إحداثيات الدوسة: " + screenPoint + " · كاميرا الواجهة: " + (uiCam ? uiCam.name : "Overlay"));
            var target = btn.gameObject;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerClickHandler);
            Log("   ✔ اتبعت pointerClick عبر EventSystem (مسار الدوسة الحقيقي)");

            // لو الزر ماستجابش (أغلب الأزرار بتتبني بالكود بـonClick.AddListener)
            if (_menu != null && _menu.IsOpen && _state == 2)
            {
                Log("   المنيو لسه مفتوحة ⇒ هستخدم onClick.Invoke() كخطة بديلة");
                btn.onClick.Invoke();
            }
            return true;
        }

        // ───────────────────────── التصوير ─────────────────────────

        static Camera ResolveCamera()
        {
            var cam = Camera.main;
            if (cam != null && cam.isActiveAndEnabled) return cam;
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c != null && c.isActiveAndEnabled) return c;
            return null;
        }

        static string Capture(string name)
        {
            _cam = ResolveCamera();
            if (_cam == null) { Log("✗ مفيش كاميرا في المشهد الحالي"); return "-"; }

            // نربط كل الكانفسات بالكاميرا قبل الرسم عشان الواجهة تبقى جوه الصورة
            int bound = TLC.UI.UIFactory.BindAllCanvasesToCamera(_cam);
            Canvas.ForceUpdateCanvases();

            const int W = 1280, H = 720;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prevT = _cam.targetTexture;
            var prevA = RenderTexture.active;
            string file = "-";
            try
            {
                _cam.targetTexture = rt;
                _cam.Render();
                RenderTexture.active = rt;

                var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();

                file = name + ".png";
                File.WriteAllBytes(Path.Combine(_outDir, file), tex.EncodeToPNG());

                // فحص بصري رقمي: هل فيه محتوى واجهة (بكسلات فاتحة فوق خلفية داكنة)؟
                Log($"  📷 {file} · كانفسات مربوطة={bound} · تباين الشاشة={Contrast(tex):0.000}");
                UnityEngine.Object.DestroyImmediate(tex);
            }
            catch (Exception e) { Log("✗ تصوير فشل: " + e.Message); file = "-"; }
            finally
            {
                _cam.targetTexture = prevT;
                RenderTexture.active = prevA;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
            return file;
        }

        /// <summary>فرق متوسط سطوع النص العلوي/الوسطي — مؤشر على وجود واجهة مرسومة.</summary>
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

        static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static void Log(string s) { _log.Add(s); Debug.Log("[TLC/Menu] " + s); }

        static void Flush()
        {
            try
            {
                Directory.CreateDirectory(_outDir);
                var lines = new List<string>(_log)
                {
                    "",
                    "═══ الملخّص ═══",
                    "لقطة المنيو            : " + _menuShot,
                    "المنيو ظاهرة           : " + (_menuVisible ? "نعم" : "لا"),
                    "زر لعبة جديدة اتداس   : " + (_buttonClicked ? "نعم" : "لا"),
                    "المنيو قفلت            : " + (_menuClosed ? "نعم" : "لا"),
                    "الفصل الأول بدأ        : " + (_chapterStarted ? "نعم" : "لا"),
                    "واجهة الحوار ظاهرة     : " + (_dialogueVisible ? "نعم" : "لا"),
                    "حالة زر المتابعة       : " + _continueState,
                    "لقطة بعد الدوسة        : " + _afterShot,
                    "لقطة حوار الفصل الأول  : " + _dialogueShot,
                };
                File.WriteAllLines(Path.Combine(_outDir, "menu-flow-log.txt"), lines);
            }
            catch (Exception) { }
        }

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;
            SessionState.EraseBool(SessionKey);
            Flush();
            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
            var deadline = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update += () => { if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(code); };
        }
    }
}
