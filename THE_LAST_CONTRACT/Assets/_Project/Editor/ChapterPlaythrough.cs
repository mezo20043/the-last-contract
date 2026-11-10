using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TLC.Narrative;

namespace TLC.EditorTools
{
    /// <summary>
    /// تشغيل الفصل الأول **بالكامل** جوه Play Mode حقيقي مع تصوير كل سطر حوار،
    /// والتقدّم أوتوماتيك في القرارات (بيختار أول قرار متاح).
    ///
    /// ده مش اختبار وحدات: ده اللاعب نفسه وهو بيمشي في الفصل، والصور من الكاميرا.
    /// الاستخدام:
    ///   Unity -projectPath . -executeMethod TLC.EditorTools.ChapterPlaythrough.Run \
    ///         -captureDir Screenshots/chapter01 -logFile play.log
    /// </summary>
    [InitializeOnLoad]
    public static class ChapterPlaythrough
    {
        const string SessionKey = "TLC_ChapterPlay";
        const string SessionDirKey = "TLC_ChapterPlayDir";
        const string SceneKey = "Assets/_Project/Scenes/Play.unity";
        const int MaxLines = 40;
        const int FramesPerLine = 6;

        static string _outDir = "Screenshots/chapter01";
        static bool _running;
        static bool _started;
        static int _lineIndex;
        static int _framesWaited;
        static NarrativeRunner _runner;
        static TLC.Bootstrap.GameBootstrap _bootstrap;
        static readonly List<string> _log = new List<string>();
        static readonly List<float> _frameTimes = new List<float>();
        static string _pendingSpeaker = "";
        static string _pendingText = "";

        static ChapterPlaythrough()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                _outDir = SessionState.GetString(SessionDirKey, "Screenshots/chapter01");
                _running = true; _started = false; _lineIndex = 0;
                EditorApplication.update += Tick;
                Debug.Log("[TLC/Play] استأنفت بعد إعادة تحميل الأسمبلي");
            }
        }

        public static void Run()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (args[i] == "-captureDir" && i + 1 < args.Length) _outDir = args[i + 1];
            Directory.CreateDirectory(_outDir);

            if (!File.Exists(SceneKey)) { Log("✗ المشهد مش موجود"); Flush(); EditorApplication.Exit(2); return; }

            EditorSceneManager.OpenScene(SceneKey);
            Log("المشهد: " + SceneKey + " · Unity " + Application.unityVersion +
                " · " + SystemInfo.graphicsDeviceName);

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

            if (!_started)
            {
                _started = true;
                Log("Play Mode بدأ — بدوّر على NarrativeRunner…");
                return;
            }

            if (Time.frameCount > 60 && Time.unscaledDeltaTime > 0f && Time.unscaledDeltaTime < 1f)
                _frameTimes.Add(Time.unscaledDeltaTime);

            if (_runner == null)
            {
                _runner = UnityEngine.Object.FindFirstObjectByType<NarrativeRunner>();
                if (_runner == null) return;
                Log("لقيت NarrativeRunner — بستنى الـBootstrap يجهّز الحالة");
                _framesWaited = 0;
                _bootstrap = UnityEngine.Object.FindFirstObjectByType<TLC.Bootstrap.GameBootstrap>();
                return;
            }

            // استنى شوية بعد بداية القصة
            if (_lineIndex == 0)
            {
                _framesWaited++;
                if (_framesWaited == 40)
                {
                    if (_bootstrap != null && !_runner.IsWaitingForInput)
                    {
                        Log("ببدأ الفصل الأول عبر GameBootstrap.StartChapter(1)");
                        try { _bootstrap.StartChapter(1); }
                        catch (Exception e) { Log("✗ StartChapter فشل: " + e.Message); }
                        return;
                    }
                    Log("حالة السرد: IsWaitingForInput=" + _runner.IsWaitingForInput + " · IsBlocked=" + _runner.IsBlocked);
                }
                if (_framesWaited < 60) return;
            }

            _framesWaited++;

            // سجّل النص الظاهر
            ReadVisibleLine();

            if (_framesWaited >= FramesPerLine && _runner.IsWaitingForInput)
            {
                CaptureLine();
                Advance();
                _framesWaited = 0;
                if (_lineIndex >= MaxLines) Finish(0);
            }
            else if (_framesWaited > 240)
            {
                // عالق (سينمائية أو انتظار تفاعل) — نتخطاه
                Log("⚠ عالق " + _framesWaited + " إطار · IsWaitingForInput=" + _runner.IsWaitingForInput + " · IsBlocked=" + _runner.IsBlocked);
                try { _runner.ResumeAfterEvent(); } catch (Exception) { }
                Advance();
                _framesWaited = 0;
                if (++_lineIndex >= MaxLines) Finish(0);
            }
        }

        static void ReadVisibleLine()
        {
            var ui = UnityEngine.Object.FindFirstObjectByType<TLC.UI.DialogueUI>();
            if (ui == null) return;
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.GetField;

            try
            {
                var speakerF = ui.GetType().GetField("_speakerLabel", flags);
                var bodyF = ui.GetType().GetField("_lineLabel", flags);
                if (bodyF != null)
                {
                    var t = bodyF.GetValue(ui) as TMPro.TextMeshProUGUI;
                    if (t != null && !string.IsNullOrEmpty(t.text)) _pendingText = t.text;
                }
                if (speakerF != null)
                {
                    var s = speakerF.GetValue(ui) as TMPro.TextMeshProUGUI;
                    if (s != null && !string.IsNullOrEmpty(s.text)) _pendingSpeaker = s.text;
                }
            }
            catch (Exception) { }
        }

        static void Advance()
        {
            try
            {
                // لو فيه اختيارات معروضة، اختار الأول
                var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var choose = _runner.GetType().GetMethod("Choose", flags);
                var choiceRootF = typeof(TLC.UI.DialogueUI);
                var ui = UnityEngine.Object.FindFirstObjectByType<TLC.UI.DialogueUI>();
                bool choicesVisible = false;
                if (ui != null)
                {
                    var f = ui.GetType().GetField("_choiceRoot", flags);
                    if (f != null)
                    {
                        var rt = f.GetValue(ui) as RectTransform;
                        choicesVisible = rt != null && rt.gameObject.activeInHierarchy && rt.childCount > 0;
                    }
                }

                if (choicesVisible && choose != null)
                {
                    choose.Invoke(_runner, new object[] { 0 });
                    Log("   ▸ اخترت القرار الأول (choices=" + "؟" + ")");
                }
                else
                {
                    _runner.Advance();
                }
            }
            catch (Exception e) { Log("   ✗ تعذر التقدّم: " + e.Message); }
        }

        static void CaptureLine()
        {
            _lineIndex++;
            var name = $"line_{_lineIndex:00}.png";
            var cam = Camera.main;
            if (cam == null)
                foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.enabled) { cam = c; break; }
            if (cam == null) { Log("✗ مفيش كاميرا"); return; }

            const int W = 1280, H = 720;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prevT = cam.targetTexture;
            var prevA = RenderTexture.active;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
                tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(_outDir, name), tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                Log($"📷 {name} · {_pendingSpeaker}: {Truncate(_pendingText, 70)}");
            }
            catch (Exception e) { Log("✗ تصوير فشل: " + e.Message); }
            finally
            {
                cam.targetTexture = prevT;
                RenderTexture.active = prevA;
                rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        static string Truncate(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "(فاضي)";
            s = s.Replace("\n", " ");
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static void Finish(int code)
        {
            _running = false;
            EditorApplication.update -= Tick;
            SessionState.EraseBool(SessionKey);

            if (_frameTimes.Count > 10)
            {
                float total = 0f;
                foreach (var t in _frameTimes) total += t;
                float avg = total / _frameTimes.Count;
                Log("── الأداء ──");
                Log("  متوسط FPS: " + (1f / avg).ToString("0.0") + " (" + SystemInfo.graphicsDeviceName + ")");
            }
            Log("── خلص: " + _lineIndex + " سطر من الفصل الأول ──");
            Flush();

            EditorApplication.isPlaying = false;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
            var deadline = EditorApplication.timeSinceStartup + 8;
            EditorApplication.update += () => { if (EditorApplication.timeSinceStartup > deadline) EditorApplication.Exit(code); };
        }

        static void Log(string s) { _log.Add(s); Debug.Log("[TLC/Play] " + s); }

        static void Flush()
        {
            try
            {
                Directory.CreateDirectory(_outDir);
                File.WriteAllLines(Path.Combine(_outDir, "playthrough-log.txt"), _log);
            }
            catch (Exception) { }
        }
    }
}
